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
| 6     | Custom fields per account (`CustomData` json)                                                  | ⏳ next       |
| 7     | Panel layout builder: tabs and sections (rename, hide, order, columns), drag and drop, preview | ⏳            |
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
- **Conditional rules** ("show X when Y") stay in code as `behaviors`; configuration can only narrow them.
- The `erp-feature` skill covers screens only. The configuration phases also change the backend, so they include
  backend work, tests and `pnpm gen:api`.

## Next: phase 6 — custom fields per account

Goal: an account can add its own fields to a screen in the Panel, and they behave like system fields in the ERP.

Plan:

1. **Data.** `FeatureFields.AccountId` (nullable; null = system field). A custom key must not clash with a system key of
   the same feature. Settings (`AccountFieldSettings`) work for custom fields as for any other field, including
   business unit overrides.
2. **Values.** A `CustomData` json column on each configurable table, starting with `Item`, keyed by field key.
   Contracts get `customData` (object) on detail and save request.
3. **Types.** text, textarea, number, decimal, money, percent, date, boolean, lookup (lookup lists only).
4. **Panel.** In Screen settings: "New custom field" (English and Bangla label, key generated from the English label,
   type, section, lookup list for a lookup field), then the usual settings. Custom fields can be edited and deactivated
   (data is kept). The type cannot change once values exist.
5. **ERP.** The resolved config marks custom fields; the engine binds them to `['customData', key]`; the view shows
   them; `toRequest` sends `customData`.
6. **Server.** `FieldRules` validates custom values (type, required, limits, lookup value exists), drops unknown keys,
   and keeps saved values of hidden or deactivated fields on update.
7. **Tests.** Unit tests for the rules, API tests for create/validate/save, Panel tests for adding a custom field.

Ask the owner before building: should custom fields appear in the list (column, filter, CSV export)? That conflicts
with the "list columns are fixed" decision above.

## Working on another machine

- Prerequisites and first run are in the README (.NET 10, Node 22, pnpm 10, SQL Server LocalDB). `pnpm install`,
  `dotnet tool restore`, then `pnpm dev:api` creates and migrates the `myErp` database and seeds the demo data;
  `pnpm dev` starts the Panel (5173) and the ERP (5174).
- After an API change, run the API and `pnpm gen:api` (or point `ERP_OPENAPI_URL` at a running API).
- When a running API locks the build output, build and test into another folder:
  `dotnet test backend/Erp.sln --artifacts-path <temp folder>`.
- Before finishing: `pnpm typecheck`, `pnpm lint`, `pnpm test:web` and `dotnet test backend/Erp.sln`.
- Run Prettier on the files you changed only, not on whole folders.

To continue, ask Claude to "start phase 6".
