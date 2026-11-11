---
name: erp-feature
description: How to build or change screens in this myErp frontend (apps/erp-web, apps/panel-web, packages/ui, packages/metadata-engine) the way the codebase already does it - module folders, lazy routes, en/bn i18n keys, feature keys and permissions, master-data list + modal pages, configuration-driven form/view/list pages, mock services when no API exists, and theme tokens. Use this whenever the user asks to add, build, extend, fix or restyle a page, feature, module, list, form, modal, filter, column, field, menu screen or translation in this ERP, even if they only name the business thing ("add a Supplier setup page", "warehouse list e filter lagao", "item form e notun field") and never say "frontend" or "feature".
---

# Building ERP features

This repo is a pnpm monorepo for a configurable ERP. The frontend is React 19 + Ant Design 5 + TanStack Query + Zustand + react-i18next. Most new work is "add a screen like the ones that already exist", so the fastest correct path is to copy the shape of the closest existing feature rather than invent one.

Read `.claude/CLAUDE.md` first if it is not already in context. Its rules (simplicity, no hardcoding, no comments) apply to everything below.

## Scope

- Work only in `apps/` and `packages/`. Do not edit or run the .NET backend (`backend/`), and do not start the API.
- Never hand-edit `packages/api-client/src/generated/`. It is produced by orval from the API's OpenAPI spec (`pnpm gen:api`), so manual edits are lost.
- If the screen needs an endpoint that does not exist in the generated client, build it on a mock service (pattern C below) so it can be swapped for the real hooks later.

## Where things live

| Path                                                  | What it holds                                                                                                                                                                                                |
| ----------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `apps/erp-web/src/modules/<module>/<area>/<feature>/` | One ERP feature: `routes.tsx`, `i18n.ts`, `pages/`, `components/`, optional `constants.ts`, `services/`, `*.css`                                                                                             |
| `apps/erp-web/src/common/`                            | App-wide `constants/routes.ts` (`featurePath`), `config/app.config.ts` (page size, stale times), hooks (`useFeatureAccess`, `useFeatureConfig`), `utils/api-errors.ts`, `utils/mock.ts`, `i18n/resources.ts` |
| `apps/panel-web/src/modules/`                         | Internal Panel screens (accounts). Same ideas, own routes and i18n                                                                                                                                           |
| `packages/ui/src/`                                    | Shared components, page templates, hooks, format utils, theme tokens, CSS. Exported from `@erp/ui`                                                                                                           |
| `packages/metadata-engine/src/`                       | Turns a feature configuration into form tabs, view tabs and list columns. Exported from `@erp/metadata-engine`                                                                                               |
| `packages/api-client/`                                | Generated React Query hooks and models (`@erp/api-client`)                                                                                                                                                   |

Before writing any component, check `packages/ui/src/index.ts` and the folder's `index.ts` files. Layouts (`ListPageLayout`, `FormPageLayout`, `DetailsPageLayout`, `WizardLayout`), overlays (`FormModal`, `AppDrawer`, `useConfirm`), fields (`Field`, `FieldGrid`, `FieldInput`, `LookupSelect`, `MoneyInput`...), tags (`StatusTag`, `SoftTag`), feedback (`PageState`, `ForbiddenResult`, `EmptyState`, `useUndoMessage`), hooks (`useListState`, `useDisclosure`, `useGoBack`, `useUnsavedGuard`) and utils (`formatMoney`, `formatDateTime`, `downloadCsv`) already exist. A new shared component goes into `packages/ui` only when a second real use appears.

## Pick the pattern

Decide which existing feature the request looks like, then open that feature's files and mirror them.

**A. Master-data setup (list + create/edit modal).** Simple records with code, name, active flag. Exemplar: `apps/erp-web/src/modules/system-setup/item-management/uom/`.

- The page renders `MasterListPage` with columns, CSV columns, filters, empty state, and the generated list, status and delete hooks.
- The modal wraps `MasterFormModal` with `Field` / `FieldGrid` and ends with `ActiveField`.
- Shared helpers are in `item-management/common/master-data.ts`: `listParams`, `useUnitKey`, `useRefreshItemManagement`, `CODE_PATTERN`, `trimmed`, `EXPORT_PAGE_SIZE`, feature keys and paths. Shared texts are the `master.*` keys in `item-management/common/i18n.ts`.

**B. Configuration-driven feature (list, full-page form, view page).** The fields of the screen come from the per-account feature configuration. Exemplar: `.../item-management/item-profile/`.

- Load the configuration with `useFeatureConfig(FEATURE_KEY)`.
- Form: `useFormTabs(config, { lookups, widgets, behaviors })` feeds `FormPageLayout`.
- View: `useViewTabs(config, record, renderers)` feeds `DetailsPageLayout`.
- List: `useConfigColumns(config, renderers)` gives the table columns and the CSV export columns.
- Do not add fields, labels or list columns in the React code. Those come from the backend manifest. `widgets` replace a field's input; `behaviors` (`watch` + `state`) can only narrow a field: hide, disable, require, filter options.
- Lookups are a hook returning `{ [lookupKey]: options }` (see `item-lookups.ts`). Request mapping and behaviors live in a plain `*-form.ts` file next to the pages.

**C. No API yet (mock service).** Exemplar: `apps/erp-web/src/modules/dashboard/services/`.

- `<name>.types.ts` holds the response types.
- `<name>.mock.ts` builds the sample data.
- `<name>.service.ts` exports `fetchX()` that returns `simulateLatency(buildX(...))`, plus a `useX()` React Query hook. Include `currentBusinessUnitId` in the query key.
- Pages only ever call `useX()`. Swapping to the real API later should then touch only the service file.

**D. Panel screens.** Exemplar: `apps/panel-web/src/modules/accounts/`. Panel keeps most of its texts in `apps/panel-web/src/common/i18n/resources.ts` and its paths in a module `constants.ts`. Follow what the Panel already does instead of the ERP layout.

## Wiring checklist (ERP)

When you add a feature, each item below is one small edit:

1. **Feature key and path.** The key is `<module>.<feature>`, e.g. `system-setup.uom`. Build the path with `featurePath('<module>', '<feature>')` from `@/common/constants/routes`. Keep both in the feature's `constants.ts`, or in the area's shared file as `master-data.ts` does. Never write a path string inline in a page.
2. **Routes.** The feature's `routes.tsx` exports a `RouteObject[]` with `lazy` page imports and the path from step 1 (`.slice(1)` drops the leading slash). Spread it into the module's `routes.tsx`. A new module also goes into `apps/erp-web/src/modules/index.ts`.
3. **i18n.** The feature's `i18n.ts` exports `<feature>Resources: AppResources` with `en` and `bn` blocks. Register it in `mergeResources(...)` in `apps/erp-web/src/common/i18n/resources.ts`.
4. **Permissions.** `const access = useFeatureAccess(FEATURE_KEY)`. Return `<ForbiddenResult />` when `!access.view`. Hide actions with `hidden: !access.create` / `edit` / `delete` / `export`. The API enforces permissions anyway; the UI just should not offer what the role cannot do.
5. **Data.** Wrap query keys with `useUnitKey(getXQueryKey(params))` so switching business unit refetches. Use `placeholderData: keepPreviousData` on lists and `enabled: access.view`. After a mutation, invalidate with the area's refresh hook.
6. **Errors.** For form saves use `applyFieldErrors(form, error)` from `@/common/utils/api-errors` to put server field errors on the inputs; otherwise show `problemMessage(error, t('master.failed'))`.

The sidebar menu comes from the API (`/api/erp/navigation`), not from frontend code. A route without a menu entry is still reachable by URL; say so to the user instead of hardcoding a menu item.

## i18n

- Every visible string is `t('<feature>.<key>')`. That includes labels, placeholders, column titles, empty states, toasts, export file names and `aria` labels.
- Add each key to both `en` and `bn` in the same edit. Write natural Bangla, not transliterated English. Keep codes and units such as `KG` or `PCS` as they are.
- Use i18next plurals (`key_one` / `key_other` with `{ count }`) and `{{name}}`-style interpolation. Never join translated pieces with `+`.
- Reuse before you add. Check `master.*` (item-management common), `common.*` (in `packages/ui/src/i18n/resources.ts`) and the feature's existing keys.
- Group keys by prefix the way existing files do: `x.title`, `x.col.*`, `x.f.*` for fields, `x.form.*` for the form.

## Styling

- Compose Ant Design components and `@erp/ui` first. Most screens need no CSS at all.
- When CSS is needed, put it in a small `<feature>.css` imported by the component that uses it. Use `erp-` class names and `var(--erp-*)` variables only: `--erp-space-*`, `--erp-radius-*`, `--erp-font-*`, `--erp-text*`, `--erp-border*`, `--erp-primary*`, `--erp-tone-*`, `--erp-tint-*`. The full list is in `packages/ui/src/theme/tokens.ts`.
- In TSX, take numbers from the tokens exported by `@erp/ui` (`space`, `radius`, `fontSize`, `sizes`, `controlHeight`) and use existing utility classes (`erp-w-full`, `erp-text-sm`, `erp-stack`, `erp-grid--*`). Do not write hex colors, raw `px` spacing or inline `style` colors.
- Status colors come from `StatusTag tone="success|warning|danger|neutral|info|violet"`. Light and dark mode both work only if colors come from tokens.

## Constants and limits

- Page size: `appConfig.defaultPageSize`. Export size: `EXPORT_PAGE_SIZE`. Stale times: `appConfig`.
- Field limits such as max length or decimal places are named constants at the top of the file (`const NAME_MAX = 50`, as in `UomFormModal.tsx`) and match the backend contract.
- Dropdown options come from the API or lookup hooks. A fixed list in code is only acceptable when no table exists yet; keep it in one constants file.

## Code style

- No comments. If one is truly unavoidable, keep it to one short line.
- Keep it small: no new abstraction, option or helper until a second real use exists. Build only what was asked, with no bonus actions, fields or settings.
- Name files the way neighbours do: `XPage.tsx`, `XFormModal.tsx`, `x.service.ts`, `x.mock.ts`, `x.types.ts`.
- Import across the app with `@/...`, and inside a feature with relative paths.

## Before you finish

Run from the repo root and fix what they report:

```
pnpm typecheck
pnpm lint
pnpm test:web
```

Do not run `pnpm test` or `pnpm test:api`, because they start the .NET tests. Then tell the user what you added, which pattern you followed, any API the screen still needs (if you used a mock), and whether a menu entry is needed on the backend side.
