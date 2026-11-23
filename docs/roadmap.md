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

In progress: user access and drafts (phases 10–16), see [user-access-plan.md](user-access-plan.md).

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

## Done: phase 10 — sessions and passwords (2026-11-15)

- Migration `UserSessions`: `UserSessions` (refresh tokens: surface, user, family, hash, expiry, rotated, revoked, IP,
  user agent), `LoginEvents`, and `MustChangePassword`, `FailedLoginCount`, `LockedUntil`, `SecurityStamp` on
  `ErpUsers` and `PanelUsers` (shared `ISignInUser`).
- `SignInService` is the one place for sign-in rules on both surfaces: password check with lockout (3 tries, 15
  minutes, `Auth:*` settings), login events, sessions, refresh rotation (reuse after `Auth:RefreshReuseGrace` ends the
  whole family), logout and "end all sessions". Access tokens last 15 minutes and carry `stamp` and `sid`;
  `SessionCheck` rejects a token whose user's stamp changed or who is inactive (30 s cache, cleared on change).
- `POST /auth/refresh`, `/auth/logout`, `/auth/change-password` on `/api/erp` and `/api/panel` (`SessionEndpoints`).
  `PasswordChangeFilter` answers 403 on every endpoint except `me`, `change-password` and `logout` while
  `MustChangePassword` is set. Password rules are `PasswordPolicy.StrongPassword()`.
- Frontend: `customFetch` refreshes once on 401 (one refresh at a time) through `refreshSession`; both stores keep the
  refresh token and treat the session as signed in while it is valid. Shared `ChangePasswordForm`,
  `NewPasswordFields` and `password-rules` in `@erp/ui`; `/change-password` page in both apps (forced or from the
  new profile menu item); sign out calls the server. `applyFieldErrors` and `problemMessage` moved to
  `@erp/api-client` for both apps.

## Done: phase 11 — username and email reset codes (2026-11-15)

- Migration `UsernameAndResetCodes`: `ErpUsers.Username` (unique, backfilled from the email) and
  `PasswordResetCodes`. `LoginRequest` now has `login`: ERP accepts email or username (`UserNames`), the Panel email.
- `IEmailSender` (`Erp.Core.Notifications`) with `SmtpEmailSender` (`Email:*` settings); in development and E2E
  emails are written to `Email:PickupDirectory` (`.mail/`, `e2e/.mail/`). Tests capture them (`CapturedEmails`).
- `PasswordResetService`: `forgot-password` emails a 6-digit code (10 minutes, 5 tries, one per minute, same answer
  for unknown users, EN or বাংলা); `reset-password` sets the password, unlocks and ends every session.
- Account setup asks for the admin's username (checked by `availability`); a password set at setup must be changed
  at first sign-in. The demo admin's username comes from `Seed:ErpAdmin:Username`.
- Frontend: `SignInForm` takes `allowUsername` and `onForgotPassword`; shared `ForgotPasswordFlow`; public
  `/forgot-password` page in both apps; profile menu **Reset password** opens it with the email filled in.

## Done: phase 12 — account admins and Panel ERP users (2026-11-15)

- Migration `AccountAdminsAndUnitFeatures`: `IsPrimaryAdmin` renamed to `IsAccountAdmin` (claim `account_admin`; an
  account may have several), `ErpUsers.Phone`, `UserBusinessUnits.RoleId` nullable and `IsActive` (a unit is offered
  at sign-in only when the row is active and has a role), and `BusinessUnitFeatures` (features turned off per unit;
  `UnitFeatureCodesAsync` limits permissions and the admin menu).
- `RequireSuperAdmin()` guards every change to an ERP user. Panel API `/api/panel/erp-users` (list with account, unit,
  type, status and search; detail with units, devices, last 50 sign-ins and access history; edit, status, reset
  password, unlock, sign out), `POST /accounts/{id}/admins` and `/accounts/{id}/business-units/{unitId}/features`.
  The last active admin of an account cannot be turned off. Changes go to `ConfigurationChanges` (`AccessAreas.User`).
- `ErpUserProfile` (`Erp.Api/Users`) holds the shared profile rules, normalising and uniqueness checks for every
  place that creates or edits an ERP user.
- Panel: new module `erp-users` (menu **ERP users**, list and details pages, `useErpUserActions` shared by both, edit,
  reset password and add admin modals). The account page has an **ERP users** tab (admins and **Add admin**) and
  **Feature access per business unit** under Menu access (`FeatureAccessTree` takes `available`).

## Done: phase 13 — ERP User Role (2026-11-15)

- `UserManagementMenu` (`Erp.Api/UserManagement`) adds System Setup › User Management with User, Employee and User
  Role. `ErpMenuCatalog` now merges modules with the same key from several providers.
- Migration `RoleStatus`: `Roles.IsActive`. Administrator is computed (`SystemRoles.IsAdministrator`): it stores no
  permissions and always gets every permission; Viewer is re-synced with the catalog at startup. An inactive role
  gives nothing.
- API `/api/erp/user-management/permission-tree` and `/roles` (list with users in the selected unit, options, get,
  create, update, status, delete, assign and remove users per unit). Users can only give permissions they have in
  the unit; account admins can give anything; nobody changes their own access.
- The demo account now also gets ERP features added after its first grant (older ones removed in the Panel stay
  removed). Other accounts get new screens only from the Panel.
- ERP: `user-management/user-role` with `RoleListPage`, `RoleFormPage` and `PermissionTree` (search with highlight,
  all / selected, expand and collapse, tri-state module, sub-module and column checkboxes, feature actions as chips;
  logic in `permission-tree.ts` with unit tests).

## Done: phase 14 — ERP Employee and User (2026-11-15)

- Migration `EmployeesAndUserLink`: `Employee` table (typed columns, `MasterRecord`), `ErpUsers.EmployeeId` (unique
  when set) and `FeatureFields.IsHiddenByDefault` (`FieldDef.Hidden`: optional fields an account turns on).
- Employee is a configurable screen (`EmployeeManifest`, tabs General / Personal / Job) with seven lookup lists
  (`EmployeeLookups`). API `/api/erp/user-management/employees` (list of the selected unit by home unit, options,
  get, create, update, status) plus `/structure/offices` and `/structure/warehouses`. Code `EMP-00001` when empty.
  Saving with `user` (Is user switch) also creates the user, linked by `EmployeeId`, in the home unit without a role.
- API `/api/erp/user-management/users`: list of the selected unit (account admins always shown, read only), options,
  unit choices, get, create with units (no role), update, add to units, role and status in this unit, sign out.
  Adding to a unit needs `system-setup.user.create` in that unit; nobody changes their own access.
- ERP screens: `user-management/employee` (list, form with the User account card, view) and `user-management/user`
  (list with Assign role, Add to units, unit status and sign out; create and edit page). The role form has an
  "Assign users" picker (`RoleUsers`). `UserProfileFields` and `UsernameField` moved to `@erp/ui` and are shared by
  the ERP and the Panel. `MasterListPage` got optional `onDelete` and `moreActions`.

## Done: phase 15 — drafts on create forms (2026-11-15)

- Migration `Drafts`: one generic table (surface, account, unit, feature, title, JSON data). API `/api/erp/drafts`
  (scoped to the account and selected unit; view needs `{feature}.view`, save and delete need `{feature}.create`)
  and `/api/panel/drafts` (shared by Panel users). Size limit `Drafts:MaxDataLength`.
- `FormPageLayout` and `FormModal` now show one split Save button; its menu holds Save & new, Save as draft and
  Cancel. `WizardLayout` has a Save as draft action. `ListPageLayout` takes `drafts` and adds a Drafts view with
  Continue and Delete (`DraftTable`).
- `useDraftRows` / `useDraftValues` in `@erp/api-client` hold the logic for both surfaces; each app wraps them in
  `common/hooks/useDrafts.ts`. Pages keep the draft in `?draft=`, modals in list state (`MasterDraftContext`).
  The draft is deleted after the record is created. Password fields are never stored in a draft.
- Covered: every ERP master modal and page (item profile, departments to UOM, employee, user, role) and in the
  Panel the account setup wizard, lookup values and custom fields. Not yet: Panel Add admin and office / warehouse
  modals (they have no list to continue from).

## Done: phase 16 — hardening (2026-11-15)

- `e2e/user-access.spec.ts`: a Panel admin builds a role and a user who signs in by username and sees only that
  screen; forgot password by emailed code; three wrong passwords lock until the Panel unlocks. E2E runs the API
  with `RateLimiting__LoginPerMinute` raised (`E2E.loginPerMinute`).
- Panel history labels for user and role changes; ESLint skips Playwright output.

## Done: phase 17 — Partner Management (2026-11-17)

- Owner asked for System Setup › Partner Management with **Customer Profile** and **Supplier Profile**, two tables,
  based on the legacy `Partner` table script. Only the profile columns were taken; balances, loyalty points,
  e-commerce, approval and HMS columns stay out until a module needs them.
- Migration `PartnerProfiles`: tables `Customer` and `Supplier` (business unit owned, `CustomData`, unique code per
  unit). Shared columns live on `PartnerRecord` (`MapPartner` in `PartnerConfigurations.cs`); Customer adds price
  type, territory, sales representative (employee), date of birth, gender, profession, marriage date; Supplier adds
  receive tolerance.
- `Erp.Api/PartnerManagement`: menu (`system-setup.customer-profile`, `system-setup.supplier-profile`, actions view,
  create, edit, export), manifests built from `PartnerFields` (tabs General, Sales & credit / Purchase terms,
  Tax & legal, and Personal for customers), lookup lists `customer-group`, `supplier-group`, `price-type`,
  `district` (64 districts), `thana` and `territory` (empty, fields hidden by default). API
  `/api/erp/partner-management/customers|suppliers` (list with group, price type, district filters; get; create;
  update; bulk status). Empty code gives `CUS-00001` / `SUP-00001`. `PartnerData` holds the shared rules.
  Customer view permission also reads employee options (sales representative).
- ERP: `system-setup/partner-management` with list, form and view pages for both; the form and view are the shared
  `PartnerForm` and `PartnerView` (profile card with click-to-call and email, place and credit summary).
- Checked with `PartnerManagementTests` (code, unit scope, wrong list, used code, hidden field kept, required field,
  custom field, bulk status), typecheck, lint, web tests and a Playwright run that created both in Chrome.

Follow-up the same day (owner asked):

- Migration `DistrictsThanasAndItemSupplier`: reference tables `District` (64) and `Thana` (545: the 495
  upazilas of the open `bangladesh-geocode` dataset plus the 50 Dhaka Metropolitan Police thanas), English and
  Bangla, shared by every account. Partner `DistrictID` / `ThanaID` now point to them (existing districts were
  remapped by name). API `/api/erp/locations/districts` and `/thanas` (`LocationEndpoints`); the partner Thana
  field has `ParentField = districtId`, so the engine narrows it by district. The `district` and `thana` lookup
  lists are gone.
- Item Profile: `supplierName` became `supplierId`, a dropdown of the unit's suppliers
  (`/partner-management/suppliers/options`); `Item.SupplierName` keeps the chosen supplier's name and
  `Item.SupplierID` has a foreign key to `Supplier`.

Left open:

- Thanas of other city corporations (Chattogram, Khulna and so on) are not in the list yet.

## Done: phase 18 — Procurement menu and Purchase Requisition (2026-11-23)

- Owner asked for a **Procurement** module: Procurement › Purchase (Purchase Requisition, Purchase Order, Purchase
  Receive, Purchase Return, Purchase Payment) and Procurement › Reports (Purchase Report). `ProcurementMenu`
  (`Erp.Api/Procurement`) declares all six; only the requisition has a screen, the others show the placeholder page.
  The requisition has an extra `approve` action; the report has view and export only.
- Migration `PurchaseRequisition`: `PurchaseRequisitionHeader` and `PurchaseRequisitionRow`, based on the legacy
  `InventoryRequestHeader` script. Only the requisition columns were taken (number, dates, warehouse, type, purchase
  method, item type, department, section, requested by, buyer, project, remarks, status, review, totals,
  `CustomData`); transfer, production order, discount, charge and VAT columns stay out until a screen needs them.
  The row table script the owner sent was a copy of the header, so the rows were designed here: item, UOM,
  quantity, rate, amount, remarks.
- Configurable screen (`PurchaseRequisitionManifest`, lookup lists `requisition-type` and `purchase-method`). API
  `/api/erp/procurement/purchase-requisitions`: list with counts by status and warehouse, type and department
  filters; item search with UOM and last cost; get; create; update (pending only); approve and reject (`approve`
  permission, reject needs a reason); close (approved only); delete (pending or rejected, soft delete). Empty
  number gives `PR-00001`. Employee and warehouse options also open to requisition users.
- ERP: `modules/procurement/purchase/purchase-requisition` (list with status views, form with an item grid, view
  with items, review and record info, approve/reject modal). `useLookupLists` and `useConfigFilters` moved to
  `common/hooks`; `useFeatureAccess` has `approve`.
- Checked with `PurchaseRequisitionTests`, typecheck, lint, web tests and a Playwright run in Chrome (create,
  validation, approve, list, placeholder page).

Left open:

- Ask the owner for the real row table script and whether attachments are needed (no file storage yet).
- The item grid is feature-local; move it to `@erp/ui` when Purchase Order needs the same grid.

Still open for later, ask the owner:

1. **Custom fields in the list** (column, filter, CSV export): needs the owner's answer from phase 6.
2. **Read-only history in the ERP** for account admins.
3. **The next business module** (for example VAT setup) built as a configurable screen (skill pattern E).

## Where we are

Last updated 2026-11-23. Phases 0 to 18 are done: account editing, user access (sessions, username and reset codes,
Panel ERP users, ERP roles, users and employees), drafts on create forms, Partner Management (customer and
supplier profiles) and the Procurement menu with Purchase Requisition. Next: Purchase Order, then "Still open for
later".

Migrations added after phase 5, all applied automatically by `pnpm dev:api`: `CustomFields`,
`MasterDataCustomFields`, `ConfigurationHistory`, `AccountTheme`, `UserSessions`, `UsernameAndResetCodes`,
`AccountAdminsAndUnitFeatures`, `RoleStatus`, `EmployeesAndUserLink`, `Drafts`, `PartnerProfiles`,
`DistrictsThanasAndItemSupplier`, `PurchaseRequisition`.

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
