# myErp — Configurable ERP

One ERP codebase whose screens and data shape are driven by configuration, per **Account**.

| App                             | Who uses it                                                                         | Dev URL                               |
| ------------------------------- | ----------------------------------------------------------------------------------- | ------------------------------------- |
| **Panel** (`apps/panel-web`)    | Internal team (Super Admin, Implementer): accounts, business units, fields, layouts | http://localhost:5173                 |
| **ERP** (`apps/erp-web`)        | Client users; UI rendered from the business unit's resolved configuration           | http://localhost:5174                 |
| **API** (`backend/src/Erp.Api`) | Both apps: `/api/panel/*` and `/api/erp/*`                                          | http://localhost:5000 (docs: `/docs`) |

## Prerequisites

- .NET SDK 10 (`dotnet --list-sdks`)
- Node.js 22+ and pnpm 10 (`npm i -g pnpm@10`)
- SQL Server LocalDB with a database named **`myErp`** (`(localdb)\MSSQLLocalDB`). Docker is not required.

## Getting started

```powershell
pnpm install                  # frontend workspace
pnpm dev:api                  # API on :5000 (runs against LocalDB / myErp)
pnpm dev                      # Panel on :5173 and ERP on :5174
```

Check the API at http://localhost:5000/health (it reports database health) and http://localhost:5000/docs (Scalar).

The connection string is in [appsettings.Development.json](backend/src/Erp.Api/appsettings.Development.json).
Override it without editing files via `ConnectionStrings__Erp`.

### Database and demo sign-in

In Development the API **applies pending EF migrations and seeds demo data on startup**
(`Database:MigrateOnStartup` / `Database:SeedDemoData`). Seeding only fills in missing rows, so restarting is safe.

| App   | Email                    | Password      | What you get                                                                          |
| ----- | ------------------------ | ------------- | ------------------------------------------------------------------------------------- |
| Panel | `superadmin@myerp.local` | `Admin@12345` | Super Admin                                                                           |
| ERP   | `admin@demopharma.com`   | `Demo@12345`  | Primary admin of **Demo Pharma Ltd**, with access to Dhaka Depot and Chattogram Depot |

These are development-only values from `appsettings.Development.json`. The login pages show a "Fill in" helper
in dev builds only.

Migrations live in `backend/src/Erp.Infrastructure/Persistence/Migrations` (`dotnet-ef` is a local tool):

```powershell
dotnet tool restore
dotnet ef migrations add <Name> --project backend/src/Erp.Infrastructure --startup-project backend/src/Erp.Api --output-dir Persistence/Migrations
dotnet ef database update      --project backend/src/Erp.Infrastructure --startup-project backend/src/Erp.Api
```

### Panel: accounts and account setup

- **The Panel sidebar is read from the `PanelMenuItems` table** (`GET /api/panel/navigation`): any depth, EN/বাংলা
  labels, an icon name from the shared icon registry, path and sort order. Startup only _inserts_ missing default
  items (currently just **Accounts**) and never overwrites edited rows. Menu entries without a built screen show a
  placeholder.
- **Accounts** (`/accounts`) has totals, a server-paged table (status tabs, search, sort) and a detail drawer with
  the business unit → office → warehouse tree.
- **New account setup** (`/accounts/new`) is one page with five steps: account info → business units → offices and
  branches per unit → warehouses per unit, either at unit level or under an office → ERP admin and review. Nothing is
  stored until **Create account**. `POST /api/panel/accounts/setup` then saves the whole tree, the system roles
  (Administrator, Viewer) and a **primary ERP admin** in one transaction. Leave the password empty and a secure one is
  generated, then shown once.
- Validation errors come back keyed by JSON path (e.g. `businessUnits[1].warehouses[0].officeCode`), and the form
  jumps to that step and field.
- Data model: `Account` (profile, base currency, time zone) → `BusinessUnit` → `Office` → `Warehouse`
  (`Warehouse.OfficeId` is optional). Offices and warehouses are business-unit-owned, so in the ERP they are filtered
  to the selected unit.

### ERP: Item Management (System Setup)

- **Master data** (`/api/erp/item-management/{departments,categories,sub-categories,minor-categories,uoms}`): the item
  hierarchy Department → Category → Sub Category → Minor Category belongs to a business unit; units of measure belong
  to the account. Each has list (status counts), options for dropdowns, create, update, bulk status and delete.
  Records that still have children cannot be deleted; deactivate them instead.
- **Item Profile** (`/api/erp/item-management/items`, table `Item`): landing list with filters and export, a create /
  edit page and a view page. The item stores all four hierarchy IDs, but the user only picks the **minor category**;
  the rest are filled in from it. Moving a category or minor category, or renaming a UOM, updates its items.
- **Item code** left empty is generated from the category code (`INJ-00001`). **Generate barcode** creates an in-store
  EAN-13 (`20` + serial + check digit); serials are never reused. Manual barcodes must be unique in the business unit.
- **Child items** point to an active master item that is not itself a child. A master item cannot be deleted while it
  has child items.
- Item types and VAT supply types are the account's **lookup lists** (`item-type`, `vat-supply-type`, see below);
  `ItemTypeID`, `PurchaseSupplyTypeID` and `SalesSupplyTypeID` point to `LookupValues`. `SupplierID`, `PurchaseVatID`
  and `SalesVatID` stay empty until supplier and VAT setup exist; the supplier is free text for now.
- The form, view and list come from the feature configuration (see below). Item type, HS code and the supply types are
  required by default but can be made optional per account, so their columns are nullable.

### Feature configuration (per account and business unit)

Every field of a feature can be shown or hidden, made required or optional, relabelled (EN/বাংলা), moved and reordered
per **account**, and again per **business unit** (branch). Business logic stays in typed tables and endpoints;
configuration only shapes the screens.

- **Definitions come from code.** A module declares a feature's tabs, sections and fields in an `IFeatureManifestProvider`
  (e.g. `ItemProfileManifest.cs`): key (camelCase, same as the API property), type, default label, help, placeholder,
  section, order, width, limits, lookup source, list column, filter, and whether the field is **locked** (always shown
  and required). Startup syncs these into `FeatureSections` and `FeatureFields`. The code owns these rows, so changes
  in a manifest overwrite them, and anything a manifest drops is deactivated.
- **Accounts override only what they change.** `AccountSectionSettings` and `AccountFieldSettings` hold one row per
  changed section or field. A null column keeps the default. Deleting the row resets it. Account settings can only
  narrow limits (max length, min, max). Locked fields stay visible and required. List columns are fixed to the ones
  the manifest declares (`ListOrder`); an account can hide or reorder them but never add another.
- **Business units override the account.** A settings row with `BusinessUnitId` applies to that unit only. Values are
  resolved column by column: manifest default → account row → business unit row (`FeatureSettings.ForUnit`).
- **Versioned.** `AccountFeatures.ConfigVersion` goes up whenever an account's configuration or a manifest changes.
  `GET /api/erp/navigation` returns each feature's `configVersion`, and the resolved configuration is cached per
  account, business unit, feature and version.
- **Panel: Screen settings** (Accounts → row menu → Screen settings, `/accounts/{id}/configuration`). Pick a screen
  and, optionally, a business unit (empty = the whole account); the table shows each field's section, type, shown,
  required, list and filter values and whether it is changed. Edit opens every setting of the field; Reset removes the
  change for that scope. API: `GET /api/panel/accounts/{id}/configuration/{code}?businessUnitId=`, and
  `PUT` / `DELETE …/fields/{key}?businessUnitId=`. The server rejects hiding or loosening a locked field, hiding a
  required field that has no default, limits beyond the manifest, list or filter on fields that have none, and unknown
  sections.
- **Lookup lists per account.** Simple dropdown lists (item type, VAT supply type…) are declared in code with an
  `ILookupProvider` (key, name and default values, e.g. `ItemLookups.cs`). Startup syncs `LookupTypes` and gives every
  account (and every new account at setup) the default `LookupValues` once: code, name EN/বাংলা, order, active and
  one optional **default** value. The default value is used for a field whose lookup is that list when neither the
  manifest nor the account sets one. Values are never deleted, only deactivated.
  - ERP: `GET /api/erp/lookups/{type}` (inactive values included and flagged, so a form can still show a saved one).
  - Panel: **Lookup lists** (Accounts → row menu → Lookup lists, `/accounts/{id}/lookups`); API
    `GET /api/panel/lookup-types`, `GET` / `POST /api/panel/accounts/{id}/lookups/{type}` and `PUT …/{valueId}`.
    Saving bumps the account's `ConfigVersion`. In Screen settings, the default of a list-lookup field is chosen from
    the list.
- **Custom fields per account.** In Screen settings, **New custom field** adds a field to a screen for one account:
  English and Bangla label, type (text, long text, number, decimal, money, percent, date, yes / no, dropdown), section
  and, for a dropdown, one of the account's lookup lists. The key is made from the English label (`Warranty months` →
  `warrantyMonths`) and, like the type, never changes. After that it is an ordinary field: shown or hidden, required,
  relabelled, moved and overridden per business unit. It can be deactivated and activated again; it is never deleted.
  - Data: `FeatureFields.AccountId` (null = system field). Values are stored in the record's `CustomData` json column
    (currently `Item`) and sent as `customData` on the detail and the save request.
  - The server checks each value (type, required, limits, an active value of the lookup list), drops keys that are not
    fields, and keeps the saved values of hidden and deactivated fields. Errors are keyed `customData.{key}`.
  - Custom fields are on the form and the view page; they are not list columns or filters.
  - API: `POST /api/panel/accounts/{id}/configuration/{code}/fields` and `PUT …/fields/{key}/status`.
- **ERP API:** `GET /api/erp/features/{code}/config` (needs `{code}.view`) returns the resolved tabs → sections →
  fields, list columns and filters. Hidden fields are left out. A feature without a manifest returns 404.
- **The server enforces it.** Save endpoints call `FeatureConfigService.ApplyRulesAsync` before their own logic: hidden
  and read-only fields get the default on create and keep the saved value on update, whatever the client sends; visible
  fields are checked for required, max length, min and max. Errors come back keyed by field. FluentValidation keeps only
  rules that never change (types, database limits, locked fields, business rules such as "barcode or auto barcode").
- **The ERP renders it.** `@erp/metadata-engine` turns the configuration into screens:
  - `useFormSections(config, { lookups, widgets, behaviors, aside })` gives `FormPageLayout` its section cards. Each
    section is a `FieldSection` grid in its own card; the sections named in `aside` go to the right column. Each field
    uses `FieldInput`, or a lookup select whose options come from `lookups[field.lookup]`.
    Lookups with a `parentField` cascade: choosing a child fills its parents, and changing a parent clears its children.
  - `widgets` replace the input of a field (e.g. the image attach button), and `behaviors` change a field at runtime from
    other values (`watch` + `state`: hidden, disabled, required, placeholder, option filter). Configuration decides
    whether a field exists for the account; behaviors can only narrow that.
  - `useViewTabs(config, record, renderers)` builds the view page: one pill tab per manifest tab, sections as read-only cards, and
    `useConfigColumns(config, renderers)` the list columns and CSV export. A lookup field shows the record's
    `…Name` property (`itemTypeId` → `itemTypeName`).
  - `useFeatureConfig(code)` in the ERP app loads the configuration, keyed by the `configVersion` from navigation.
  - A custom field (`field.custom`) is bound to `['customData', key]`. `useCustomLookups(config)` loads the lookup
    lists that custom dropdowns use; pass its result to `useFormSections` and `useViewTabs`.

### Authentication model

- **Two surfaces, two audiences.** Panel tokens (`aud=panel`) only work on `/api/panel/*`, and ERP tokens
  (`aud=erp`) only on `/api/erp/*`. Each surface has its own bearer scheme, so a token for one gets **401** on the other.
- **ERP tokens are account-scoped.** The active business unit travels in the **`X-Business-Unit`** header and is
  validated on every request against `UserBusinessUnit` (the account's primary admin may use every active unit).
  Switching units therefore needs no new token.
- **Tenant isolation is a global EF query filter.** Entities implementing `IAccountOwned` / `IBusinessUnitOwned`
  are filtered to the current account and unit. With no tenant in context the filter matches nothing, so
  code that must cross tenants (login, seeding, Panel) uses `IgnoreQueryFilters()` explicitly.
- **Permissions** are `{module}.{feature}.{action}` codes contributed by modules through `IPermissionProvider`
  and synced to the `Permissions` table at startup. Endpoints declare `.RequirePermission("inventory.item.create")`:
  a missing business unit returns 400, and a missing permission returns 403. The UI hides what a role can't do,
  but the API is the actual protection.
- Login endpoints are rate-limited per IP (`RateLimiting:LoginPerMinute`, default 10). Unknown email, wrong
  password and disabled users all get the same 401.
- The JWT signing key in `appsettings.Development.json` is for development only. Set `Jwt__SigningKey`
  (≥ 32 characters) in every other environment.

## Scripts

| Script                                         | What it does                                                                                             |
| ---------------------------------------------- | -------------------------------------------------------------------------------------------------------- |
| `pnpm dev`                                     | Both frontends with HMR                                                                                  |
| `pnpm dev:api`                                 | ASP.NET Core API (`http` launch profile, port 5000)                                                      |
| `pnpm gen:api`                                 | Regenerates `packages/api-client` from `http://localhost:5000/openapi/v1.json` (**API must be running**) |
| `pnpm test`                                    | Vitest (all packages) + `dotnet test`                                                                    |
| `pnpm typecheck` / `pnpm lint` / `pnpm format` | Static checks                                                                                            |
| `pnpm build`                                   | Production build of both apps                                                                            |

API integration tests boot the real API (migrations and seed included) against a throwaway **`myErp_Test`**
LocalDB database. Tenant-filter tests use **`myErp_Test_Tenancy`**. Both are created and dropped by the tests,
so your `myErp` database is never touched. Set `ERP_TEST_CONNECTION` to use a different server.

## Repository layout

```
backend/
  Erp.sln, Directory.Build.props, Directory.Packages.props   (central package versions)
  src/Erp.Api               Minimal API host: Serilog, ProblemDetails, CORS, OpenAPI + Scalar, /health
  src/Erp.Core              Domain primitives, tenancy and permission abstractions
  src/Erp.Metadata          Feature manifests, manifest catalog, configuration builder (defaults + account settings)
  src/Erp.Infrastructure    EF Core (ErpDbContext), migrations, persistence
  src/Modules/Inventory     Inventory module; each feature's endpoints, contracts and manifest live together
  tests/Erp.Tests           xUnit + WebApplicationFactory against LocalDB
apps/
  panel-web                 Panel (Vite + React 19 + AntD 5)
  erp-web                   ERP client
packages/
  ui                        Common library: components, layout (AppShell), theme, i18n (EN/বাংলা), hooks, utils
  api-client                orval-generated TanStack Query hooks + fetch client (token, X-Business-Unit)
  metadata-engine           Form, view and list built from a feature configuration (generic, no feature code)
```

Project references: `Api → all`, `Modules → Core, Metadata`, `Infrastructure → Core, Metadata`, `Metadata → Core`.

## Frontend structure

`packages/ui` (`@erp/ui`) is the common library both apps share. Build screens from it; avoid one-off styling.

```
packages/ui/src/
  theme/design-tokens.ts  every colour, size, radius and spacing (the only place to change the look)
  layout/templates/       ListPageLayout, FormPageLayout, DetailsPageLayout, WizardLayout
  layout/app-shell/       sidebar, rail, top bar, Ctrl K search      layout/page/  PageContainer, CardGrid
  layout/auth/            LoginLayout, SignInForm
  components/actions/     ActionItem config + ActionButton / ActionGroup (buttons, menus, confirm, links)
  components/overlays/    AppModal, FormModal, AppDrawer, useConfirm
  components/             fields, table, filters, dropdown, data-display, charts, upload, feedback
  hooks/                  useListState, useDisclosure, useUnsavedGuard, useHotkey
  utils/                  format (৳, lakh/crore, dates), CSV export, menu helpers
  styles/                 shell.css + components.css, using CSS variables only
```

Each app follows the same layout:

```
apps/<app>/src/
  main.tsx
  app/          App, router, layouts (ShellLayout)
  common/       config (app.config.ts, .env values), constants, guards, services, store, navigation, i18n, pages,
                master-data (ERP: list page, form modal and helpers every code/name/active screen shares)
  modules/      <module>/<sub-module>/<feature>/{pages,components,services,types,i18n.ts,routes.tsx,constants.ts}
```

How to build a feature:

- **Landing page** — `useListState` + `ListPageLayout` (filters, quick views, columns, bulk actions as config). Row
  actions sit in the last column: view and edit as icons, everything else in a ⋮ menu. Clicking a row opens the
  record. Code/name/active records use `MasterListPage` from `apps/erp-web/src/common/master-data`.
- **Create / edit** — `FormPageLayout` (section cards, an optional right column, submit actions). Save lives in the
  sticky page header (Cancel, Save & new, Save; Ctrl+S). The first invalid field is scrolled into view and focused;
  dirty state and the leave-page guard are built in. Small records use `MasterFormModal` / `FormModal`, with
  "Create & add another". Multi-step creation uses `WizardLayout` (vertical step list from the `lg` breakpoint).
- **View / details** — `DetailsPageLayout` (profile card on the left, read-only section cards on the right, loading and
  not-found states).
- **Configurable feature** — add a manifest on the backend, then build the form, view and columns with
  `@erp/metadata-engine` instead of hand-written sections (example: Item Profile).
- **UI and UX standard** — the full rules (layout, row actions, form spacing, UX) are in
  `.claude/skills/erp-feature/SKILL.md`.
- **Buttons** — pass `ActionItem` objects (`label`, `icon`, `variant`, `to`, `onClick`, `confirm`, `menu`) instead of hand-made buttons.
- **Modals** — `AppModal` / `FormModal` with `useDisclosure()`; confirmations with `useConfirm()`.

Modules never import from `app/`. Register routes in `modules/index.ts` and translations in `common/i18n/resources.ts`.
Example feature: `apps/erp-web/src/modules/system-setup/item-management/item-profile/`.

Features call the generated hooks from `@erp/api-client` (`pnpm gen:api` after an API change). Only the dashboard
still reads in-browser sample data (`modules/dashboard/services/*.mock.ts`) until its APIs exist.

## Theme system

Both apps share one theme engine in `packages/ui/src/theme`:

- **Presets** (`presets.ts`) follow the design kit: **Teal** `#0F766E` (default), Indigo, Blue, Violet, Emerald, Rose,
  Orange and Slate, plus a **custom** colour picker. Every preset primary carries white text at WCAG AA contrast.
- **`buildBrand(preset, mode, sidebarStyle)`** derives every other colour: the app frame and inset panel, borders,
  table header and hover rows, sidebar states and the deep brand shade. Unit tests check text contrast in
  **every preset × light/dark × sidebar style**. `UiProvider` publishes the brand as CSS variables (`--erp-*`, `--sb-*`).
- **Theme popover** (top bar): primary colour, sidebar style (**Light** = shares the app frame, **Dark**, **Brand**),
  Light / Dark / System mode and compact density. Choices are saved per browser.
- Components read colours with `useBrand()` (or AntD tokens). To add a preset, append a `{ key, name, primary, accent }`
  entry. The contrast test fails if the primary is too light for white text.

## Conventions

- **Navigation is three levels:** Module › Sub-module › Feature (e.g. Inventory › Master Data › Item Profile),
  plus top-level entries (Dashboard, Reports Center, Settings, Help). Menu nodes are `MenuNode` objects in
  `packages/ui`. Only leaves have routes.
- **Feature key** = `{module}.{feature}` (e.g. `inventory.item`); route = `/app/{module}/{feature}`.
- **Permission code** = `{module}.{feature}.{action}` (e.g. `inventory.item.create`), enforced by the API.
- No industry-specific branches in code; behaviour differences come from configuration only.
- `packages/metadata-engine` must stay generic; it never references a concrete feature.
- Labels that come from metadata are `LocalizedText` (`{ en, bn }`); UI strings use i18next keys.

## Version pins and machine notes

- **antd 5** (not 6), because `@ant-design/pro-components` 2.x supports only antd 5. React 19 support comes from
  `@ant-design/v5-patch-for-react-19`, which `packages/ui` imports.
- **React Router 7**, **TypeScript 6.0** (typescript-eslint does not support TS 7 yet).
- pnpm is also a root devDependency, so nested `pnpm` calls inside scripts use `node_modules/.bin`. Windows
  user-profile paths containing `&` break npm's global `pnpm.cmd` shim.
- Stop the running API before `dotnet build` or `dotnet test`, because the running process locks `Erp.Api.dll`.

## Roadmap

Phase status, the decisions behind them and the plan for the next phase are in [docs/roadmap.md](docs/roadmap.md).
