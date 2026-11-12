# Roadmap and handoff

Read this before starting or continuing a phase. Update it when a phase is done: the status table, the decisions and
the plan for the next phase. How each part works today is described in the [README](../README.md).

## Goal

Every screen of the ERP is configurable: fields, lookups, required or optional, shown or hidden, labels (EN/বাংলা) and
layout. The internal team sets this up in the **Panel**, per **account** and per **branch** (= business unit), screen by
screen (module › sub-module › feature). A few fields are mandatory and locked; everything else starts from a default and
can be changed.

## Status

| Phase | Scope                                                                                          | Status        |
| ----- | ---------------------------------------------------------------------------------------------- | ------------- |
| 0     | Repo skeleton, API host, LocalDB health, shared UI shell (3-level menu, dark mode, EN/বাংলা)   | ✅ 2026-10-31 |
| 1     | Core domain, migrations, JWT auth (panel/erp audiences), tenant context, login + BU picker     | ✅ 2026-11-01 |
| 2     | Feature manifests, field/section tables, account settings, resolved config API, versioning     | ✅ 2026-11-08 |
| 3     | `@erp/metadata-engine` (form, view, list from config); Item Profile on config; server rules    | ✅ 2026-11-09 |
| 4     | Business unit overrides; Panel **Screen settings** (show/hide, required, labels, order, list)  | ✅ 2026-11-11 |
| 5     | Lookup lists per account (`LookupTypes`/`LookupValues`), lookup API, Panel **Lookup lists**    | ✅ 2026-11-12 |
| 6     | Custom fields per account (`CustomData` json), Panel **New custom field**                      | ✅ 2026-11-12 |
| 7     | Panel layout builder: tabs and sections (rename, hide, order, columns), drag and drop, preview | ✅ 2026-11-12 |
| 8     | Item master data screens (department → UOM) on the engine                                      | ✅ 2026-11-12 |
| 9     | Hardening: E2E, audit log (Panel History), docs                                                | ✅ 2026-11-12 |

Not scheduled yet, on hold by the owner: ERP module › sub-module › feature permissions per role.

## Decisions to keep

- **Typed tables stay.** Business data lives in real tables and endpoints; configuration only shapes screens and
  validation. No EAV or generic entity API.
- **Manifests are C#** (`IFeatureManifestProvider`, `ILookupProvider`) and own the system rows. Account rows only store
  what changed; a null column means "use the default".
- **Branch = business unit**, not office. Values resolve column by column: manifest default → account → business unit.
- **Locked fields** are always shown and required. A required field can be hidden only when it has a default.
- **List columns are fixed** to the columns the manifest declares (the ones the list table really has). The Panel can
  hide or reorder them but never add another column (owner, 2026-11-09).
- **Lookup lists are per account** (same for every branch). Values are never deleted, only deactivated. One optional
  default value per list; it is the default of every field using that list unless a screen sets its own.
- **Custom fields are per account** (`FeatureFields.AccountId`; null = system field) and added at account level only.
  Their values live in one `CustomData` json column of the record, keyed by field key, and travel as `customData`
  on the detail and save contracts. Everything else (settings, business unit overrides, rules) is the same as for a
  system field.
- **A custom field's key and type never change.** The key comes from the English label when the field is created. To
  change the type, deactivate the field and add a new one. Fields are never deleted, only deactivated; saved values
  stay in `CustomData` and come back when the field is activated again.
- **Custom fields are not list columns or filters** (owner has not decided yet, so "list columns are fixed" wins).
  They show on the form and the view page only.
- **Tabs and sections come only from the manifest.** An account can rename, hide, reorder and resize them and move a
  section to another tab, but never add its own tab or section (owner, 2026-11-12).
- **Every screen is built from its configuration, generically.** Forms and modals render `ConfigFields`: one tab
  shows its sections, two or more tabs show tabs. A screen that has one tab today gets tabs later by adding a tab to
  its manifest, with no screen code change (owner, 2026-11-12).
- **Configuration history is append-only and shown in the Panel only** for now; the ERP may get a read-only view
  later (owner, 2026-11-12). A save that changes nothing writes no history row.
- **E2E runs on its own database** (`myErp_E2E`) and ports (API 5065, Panel 5193, ERP 5194) with the installed Chrome,
  so it never touches the dev data and needs no browser download.
- **Accounts are edited, never deleted.** Profile and structure can change after setup; the account code and the ERP
  admin cannot. Business units, offices and warehouses are deactivated, not deleted (owner, 2026-11-12).
- **The account theme is a starting point.** The ERP applies it at sign-in; each user can still change the look in
  the browser afterwards.
- **Drag and drop uses dnd-kit** (`@dnd-kit/core`, `@dnd-kit/sortable`), in the Panel only (owner, 2026-11-12).
- **A layout save stores only what differs from the level below.** Dragging a field back to its default place removes
  that override again, so an untouched screen keeps no rows.
- **Conditional rules** ("show X when Y") stay in code as `behaviors`; configuration can only narrow them.
- The `erp-feature` skill covers screens only. The configuration phases also change the backend, so they include
  backend work, tests and `pnpm gen:api`.

## Done: phase 6 — custom fields per account

What was built (details are in the README, "Custom fields per account"):

- Migration `CustomFields`: `FeatureFields.AccountId` and `Item.CustomData`.
- `FieldRules` checks custom values (type, required, limits, active lookup value), drops unknown keys and keeps the
  saved values of hidden and deactivated fields. Errors are keyed `customData.{key}`.
- Panel API: `POST …/configuration/{code}/fields` and `PUT …/fields/{key}/status`; Screen settings has
  **New custom field** and Activate / Deactivate in the row menu.
- Engine: custom fields bind to `['customData', key]`, the view page shows them, and a `date` input now holds a
  `YYYY-MM-DD` string. `useCustomLookups(config)` in the ERP loads the lookup lists custom fields use.

To give another screen custom fields: add a `CustomData` string column to its table, `customData` to its detail and
save contracts (see `ItemContracts.cs` and `ItemEndpoints.cs`), send `customData` from the form's `toRequest`, and
pass `useCustomLookups(config)` to `useFormSections` and `useViewTabs`.

Left open:

- The Panel and ERP screens were checked by typecheck, lint and tests only, not by hand in a browser. Try once: add a
  custom field of each type in Screen settings, then create, edit and view an item.
- Ask the owner: should custom fields appear in the list (column, filter, CSV export)?
- There is no Panel component test for the new modal; the API tests cover the rules.

## Done: phase 7 — Panel layout builder

What was built (details are in the README, "Panel: Layout"):

- Panel API on `/api/panel/accounts/{id}/configuration/{code}`: the editor now also returns `layout` (tabs and
  sections with base and own values); `PUT` / `DELETE /sections/{key}` renames, hides, resizes or resets a tab or
  section; `PUT /layout` saves the whole order of tabs → sections → fields in one call; `GET /preview` returns the
  screen as the ERP gets it. All take `?businessUnitId=`.
- The server rejects a change that would hide a locked field, or a required field without a default, whether by
  hiding a tab or section or by moving the field into a hidden one.
- Panel: Screen settings → **Layout** (`/accounts/{id}/layout?feature=&unit=`). Drag tabs, sections and fields
  (keyboard works too); each drop saves at once. The pencil opens the tab or section modal; **Preview** shows the form
  in a drawer. The drag logic is plain functions in `layout-model.ts` with unit tests.

Left open:

- Checked by typecheck, lint, unit and API tests only, not by hand in a browser (the browser extension was not
  connected). Try once: drag a field to another section, move a section to another tab, hide a section, open Preview,
  then open the item form in the ERP.
- Custom field questions from phase 6 are still open (list column, filter, CSV).

## Done: phase 8 — item master data screens on the engine

What was built:

- `ItemMasterDataManifest.cs` declares Department, Category, Sub Category, Minor Category and UOM: one tab
  (`general`) and one section (`details`) each. Code, name and the saved parent are locked; the parent filters
  (department on sub and minor category, category on minor category) are optional lookups with `ParentField`.
- Migration `MasterDataCustomFields`: `CustomData` on the five tables. Rows and save requests carry `customData`.
- Every create and update calls `MasterData.ConfiguredAsync` (configuration rules) before its own checks, so hidden
  fields keep their value and required ones are enforced, exactly like Item Profile.
- List sorting accepts field keys (`departmentId` sorts by department) through `MasterData.SortKey`.
- ERP: `ConfigFields` (metadata engine) renders any configuration inside a form: sections, or tabs when there are
  two or more. `MasterFormModal` now takes `config` and renders `ConfigFields` with the image widget and custom
  field lookups built in. `useMasterColumns` builds list columns and CSV from the configuration (name cell with
  image included); a page adds only its computed columns (child counts).
- The five modals shrank to "initial values + request mapping"; the cascading parent dropdowns now come from the
  engine. `ITEM_LOOKUPS` moved to `item-management.ts`. Unused i18n keys were removed.

Left open:

- Code inputs no longer turn upper case while typing; the server and the request still upper-case department and
  category codes.
- Custom field questions from phase 6 are still open (list column, filter, CSV).

## Done: phase 9 — hardening

What was built:

- **History.** Table `ConfigurationChanges` (migration `ConfigurationHistory`): account, business unit, feature,
  area (field, section, layout, custom-field, lookup-value), key, before and after json; who and when come from
  `CreatedBy` / `CreatedAt`. `ConfigurationHistory.Record` is the one place that writes it, called from
  `FeatureConfigurationService` and the lookup endpoints in the same save. API
  `GET /api/panel/accounts/{id}/history?feature=&businessUnitId=&area=&page=&pageSize=`.
- **Panel: History** (Screen settings → History, `/accounts/{id}/history`): filters by screen, business unit and
  change type; a row opens a drawer with each changed setting, before and after (`history.ts`, unit tested).
- **E2E.** `pnpm test:e2e` (Playwright, `e2e/`) starts its own API and both apps, then: Panel adds a custom field
  to Item Department → History shows it → ERP creates a department with a value in it → reopening keeps the value.
  This is also the first check of phases 6 to 8 in a real browser, and it passes.
- **Docs.** The `erp-feature` skill now describes config-driven master data modals (pattern A) and how to add a new
  configurable screen end to end (pattern E).

Left open:

- E2E covers one flow. Layout drag and drop and Item Profile are not in E2E yet.
- Custom field questions from phase 6 are still open (list column, filter, CSV).

## Done: account editing, structure view, screen configuration and ERP theme (2026-11-12)

- **Account page** `/accounts/{id}` replaces the detail drawer: profile, admin, structure and menu access.
- **Edit account** `/accounts/{id}/edit` reuses the setup wizard's `AccountStep` (`lockedCode`), plus active /
  inactive. API `PUT /api/panel/accounts/{id}` (`AccountEditEndpoints.cs`).
- **Structure view** `StructureView.tsx` replaces the old tree everywhere (account page, setup summary, review): unit
  cards with office cards and warehouse rows. On the account page it adds and edits nodes through
  `StructureNodeModal` and `POST` / `PUT /api/panel/accounts/{id}/structure`.
- **Screen configuration** `/configuration` (Panel menu, seeded): account picker and links to every configuration
  tool of that account.
- **ERP theme** `/accounts/{id}/theme`: migration `AccountTheme` (`Accounts.Theme` json). The ERP login response
  carries `theme`; `applyAccountTheme` sets the theme store at sign-in.
- Checked with an API test, typecheck, lint, unit tests, the E2E flow and screenshots of every new page in Chrome.

## Next: owner to choose

Candidates, in no fixed order:

1. **ERP permissions per role** (module › sub-module › feature actions), on hold by the owner.
2. **Edit the ERP admin** (name, email, password reset), left out of account editing on purpose.
3. **Custom fields in the list** (column, filter, CSV export): needs the owner's answer from phase 6.
4. **Read-only history in the ERP** for account admins.
5. **The next business module** (for example suppliers or VAT setup) built as a configurable screen (skill pattern E).

Ask the owner which one comes next before starting.

## Where we are

Last updated 2026-11-12. Phases 0 to 9 and account editing (account page, structure view, Screen configuration menu,
ERP theme per account) are done. Nothing is half built. The next step is the owner's choice from "Next: owner to
choose" above.

Migrations added after phase 5, all applied automatically by `pnpm dev:api`: `CustomFields`,
`MasterDataCustomFields`, `ConfigurationHistory`, `AccountTheme`.

## Working on another machine

After `git pull` on the other machine:

1. `pnpm install` (new packages since phase 6: `@dnd-kit/*` in the Panel, `@playwright/test` at the root).
2. `dotnet tool restore` (only the first time).
3. Stop any running API, then `pnpm dev:api`. It applies the new migrations to `myErp`, adds missing Panel menu items
   (Accounts, Screen configuration) and seeds the demo data. Then `pnpm dev`.
4. Optional check: `pnpm test:web`, `dotnet test backend/Erp.sln` and `pnpm test:e2e` (needs Chrome and LocalDB).
5. Tell Claude: "read docs/roadmap.md and start <item> from Next" (or "continue where we stopped").

The Claude memory of one machine does not travel with git; the rules that matter are in `.claude/CLAUDE.md` and in
the "Decisions to keep" above.

- Prerequisites and first run are in the README (.NET 10, Node 22, pnpm 10, SQL Server LocalDB). `pnpm install`,
  `dotnet tool restore`, then `pnpm dev:api` creates and migrates the `myErp` database and seeds the demo data;
  `pnpm dev` starts the Panel (5173) and the ERP (5174).
- After an API change, run the API and `pnpm gen:api` (or point `ERP_OPENAPI_URL` at a running API).
- When a running API locks the build output, build and test into another folder:
  `dotnet test backend/Erp.sln --artifacts-path <temp folder>`.
- Before finishing: `pnpm typecheck`, `pnpm lint`, `pnpm test:web` and `dotnet test backend/Erp.sln`; after UI
  flow changes also `pnpm test:e2e` (needs Chrome and LocalDB; it starts its own API and apps).
- Run Prettier on the files you changed only, not on whole folders.

To continue, tell Claude which item of "Next: owner to choose" to start. Demo sign-ins are in the README.
