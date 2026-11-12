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

## Roadmap

- Phase status, decisions and the next phase plan are in `docs/roadmap.md`. Read it before starting or continuing a phase, and update it when a phase is done.
- When starting on a new machine or a new session, read the "Where we are" and "Working on another machine" sections of `docs/roadmap.md` first.

## Working rules

- Plain screen work stays in `apps/` and `packages/` and uses mock services when no API exists. Roadmap phases and configurable screens include the backend: code, migration, tests and `pnpm gen:api`.
- Do not log work from this repository to DevOS, even if a global instruction asks for it.
- If `pnpm dev:api` is already running, do not stop it. Build and test into another folder (`--artifacts-path <temp folder>`) and run a second API on another port for `pnpm gen:api` (`ERP_OPENAPI_URL`).
- Run Prettier only on the files you changed. If it reformats a file you did not mean to touch, revert that file.
- Check UI changes in a real browser with Playwright (`pnpm test:e2e`, or a temporary spec in `e2e/` that takes screenshots), then delete the temporary spec.
- Ask the owner before building anything a roadmap item marks as "ask the owner".

## Comments

- Do not add comments to code.
- If a comment is truly needed, write a single short, simple line.
- No JSDoc blocks, section banners or multi-line explanations.
