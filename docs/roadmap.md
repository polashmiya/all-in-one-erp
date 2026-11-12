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
| 7     | Panel layout builder: tabs and sections (rename, hide, order, columns), drag and drop, preview | ⏳ next       |
| 8     | Item master data screens (department → UOM) on the engine                                      | ⏳            |
| 9     | Hardening: E2E, audit log, docs                                                                | ⏳            |

Not scheduled yet, on hold by the owner: ERP module › sub-module › feature permissions per role, and editing or
deactivating an account after setup.

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

## Next: phase 7 — Panel layout builder

Goal: the internal team arranges a screen in the Panel: rename, hide and reorder tabs and sections, set a section's
columns, drag fields between sections, and see a preview before users do.

What already exists: `AccountSectionSettings` (label, order, columns, visible, parent tab) with account and business
unit rows, and `FeatureConfigBuilder` already applies them. Only the Panel API and the screen are missing.

Plan:

1. **API.** The editor (`GET …/configuration/{code}`) returns tabs and sections with their base and own values, like
   fields do. Add `PUT` / `DELETE …/sections/{key}?businessUnitId=` (label EN/বাংলা, order, columns 1 to 4, visible,
   tab for a section). Reject hiding a tab or section that holds a locked field, or a required field without a
   default. Bump `ConfigVersion`.
2. **Preview.** A Panel endpoint that returns the resolved `FeatureConfig` for an account and optional business unit
   (reuse `FeatureConfigService.GetAsync`). The Panel renders it with `useFormSections`, inputs disabled.
3. **Panel.** A "Layout" view in Screen settings: tabs → sections → fields. Drag to reorder tabs, sections and fields
   and to move a field to another section (saves `sortOrder` and `sectionKey` through the existing field endpoint).
   Rename, hide and set columns in a small modal. Reset per tab or section.
4. **Tests.** Unit tests for section rules, API tests for save, reset and business unit override.

Ask the owner before building: may an account add its own tabs and sections, or only rearrange the ones the manifest
declares? And which drag and drop library to add (none is installed yet).

## Working on another machine

- Prerequisites and first run are in the README (.NET 10, Node 22, pnpm 10, SQL Server LocalDB). `pnpm install`,
  `dotnet tool restore`, then `pnpm dev:api` creates and migrates the `myErp` database and seeds the demo data;
  `pnpm dev` starts the Panel (5173) and the ERP (5174).
- After an API change, run the API and `pnpm gen:api` (or point `ERP_OPENAPI_URL` at a running API).
- When a running API locks the build output, build and test into another folder:
  `dotnet test backend/Erp.sln --artifacts-path <temp folder>`.
- Before finishing: `pnpm typecheck`, `pnpm lint`, `pnpm test:web` and `dotnet test backend/Erp.sln`.
- Run Prettier on the files you changed only, not on whole folders.

To continue, ask Claude to "start phase 7".
