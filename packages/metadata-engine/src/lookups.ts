import type { ItemLookup } from '@erp/api-client';

export type LookupSources = Record<string, ItemLookup[] | undefined>;

export const lookupLabel = (item: ItemLookup) =>
  item.code ? `${item.name} (${item.code})` : item.name;

export function selectOptions(items: ItemLookup[] | undefined, current?: unknown) {
  return (items ?? [])
    .filter((item) => item.isActive || item.id === current)
    .map((item) => ({ value: item.id, label: lookupLabel(item) }));
}

export function childSelectOptions(
  items: ItemLookup[] | undefined,
  parents: ItemLookup[] | undefined,
  parentId: unknown,
  current?: unknown,
) {
  const visible = (items ?? []).filter(
    (item) =>
      (item.isActive || item.id === current) && (parentId == null || item.parentId === parentId),
  );
  const toOption = (item: ItemLookup) => ({ value: item.id, label: lookupLabel(item) });
  if (parentId != null) return visible.map(toOption);

  return (parents ?? [])
    .map((parent) => ({
      label: parent.name,
      title: parent.name,
      options: visible.filter((item) => item.parentId === parent.id).map(toOption),
    }))
    .filter((group) => group.options.length > 0);
}

export const parentOf = (items: ItemLookup[] | undefined, id: unknown) =>
  id == null ? undefined : (items?.find((item) => item.id === id)?.parentId ?? undefined);
