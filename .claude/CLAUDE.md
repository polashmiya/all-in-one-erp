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

## Comments

- Do not add comments to code.
- If a comment is truly needed, write a single short, simple line.
- No JSDoc blocks, section banners or multi-line explanations.
