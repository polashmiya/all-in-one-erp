# Project rules

## Simplicity

- Keep code simple, small and readable. Prefer the plainest solution that works.
- No over-engineering: no extra abstractions, layers, options or generic helpers until a real second use exists.
- Reuse what the project already has before adding new code or dependencies.
- Build only what was asked. Do not add extra features, actions, fields or options on your own.
- Keep things easy to manage: one place for each piece of logic, shared components for shared UI, no copy-paste.

## No hardcoding

- UI text always comes from i18n keys (English and Bangla), never literal strings in components.
- Colors, spacing, sizes and fonts come from theme tokens / CSS variables, never raw values.
- Routes, API paths, feature keys, limits and page sizes come from existing constants or config, never inline literals.
- Lists and options come from the database, API or lookups. Use fixed lists in code only when no table exists yet.

## Same structure for every feature

- Build every screen from the shared templates: `ListPageLayout`, `FormPageLayout`, `DetailsPageLayout`, `WizardLayout` and `FormModal` in `@erp/ui`, plus the master-data kit in `apps/erp-web/src/common/master-data`. Never hand-build a page header, table, save bar, stepper or row-action column.
- A feature folder always has the same files: `routes.tsx`, `i18n.ts`, `constants.ts`, `pages/`, `components/`, and `services/` only when it needs a mock.
- A change that should apply to every screen goes into the shared template, not into one feature.
- Follow the "UI and UX standard" in `.claude/skills/erp-feature/SKILL.md` for layout, row actions, form spacing and UX rules.

## Comments

- Do not add comments to code.
- If a comment is truly needed, write a single short, simple line.
- No JSDoc blocks, section banners or multi-line explanations.
