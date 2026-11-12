import { arrayMove } from '@dnd-kit/sortable';
import type {
  FeatureConfigEditor,
  FieldConfigRow,
  LayoutRequest,
  SectionConfigRow,
} from '@erp/api-client';
import { effective } from '../account-configuration/field-settings';

export type NodeKind = 'tab' | 'section' | 'field';

export interface SectionNode {
  key: string;
  row: SectionConfigRow;
  fields: FieldConfigRow[];
}

export interface TabNode {
  key: string;
  row: SectionConfigRow;
  sections: SectionNode[];
}

export const DROP_TARGETS: Record<NodeKind, NodeKind[]> = {
  tab: ['tab'],
  section: ['section', 'tab'],
  field: ['field', 'section'],
};

export const nodeId = (kind: NodeKind, key: string) => `${kind}:${key}`;

export function parseId(id: string) {
  const [kind, key] = id.split(':');
  return { kind: kind as NodeKind, key: key ?? '' };
}

const orderOf = (row: FieldConfigRow | SectionConfigRow) => effective(row).sortOrder ?? 0;

const byOrder = (a: FieldConfigRow | SectionConfigRow, b: FieldConfigRow | SectionConfigRow) =>
  orderOf(a) - orderOf(b);

export function buildLayout(editor: FeatureConfigEditor): TabNode[] {
  const fields = editor.fields.filter((field) => field.isActive).sort(byOrder);
  const sections = editor.layout.filter((row) => !row.isTab).sort(byOrder);

  return editor.layout
    .filter((row) => row.isTab)
    .sort(byOrder)
    .map((tab) => ({
      key: tab.key,
      row: tab,
      sections: sections
        .filter((section) => effective(section).tabKey === tab.key)
        .map((section) => ({
          key: section.key,
          row: section,
          fields: fields.filter((field) => effective(field).sectionKey === section.key),
        })),
    }));
}

function relocate<T extends { key: string }>(
  lists: T[][],
  key: string,
  target: number,
  overKey?: string,
): T[][] {
  const source = lists.findIndex((list) => list.some((item) => item.key === key));
  const item = lists[source]?.find((entry) => entry.key === key);
  const destination = lists[target];
  if (!item || !destination) return lists;

  const index = overKey
    ? destination.findIndex((entry) => entry.key === overKey)
    : destination.length;
  if (source === target)
    return lists.map((list, i) =>
      i === source ? arrayMove(list, list.indexOf(item), Math.min(index, list.length - 1)) : list,
    );

  return lists.map((list, i) => {
    if (i === source) return list.filter((entry) => entry !== item);
    if (i === target) return [...list.slice(0, index), item, ...list.slice(index)];
    return list;
  });
}

export function moveNode(tabs: TabNode[], activeId: string, overId: string): TabNode[] | null {
  if (activeId === overId) return null;
  const active = parseId(activeId);
  const over = parseId(overId);
  if (!DROP_TARGETS[active.kind].includes(over.kind)) return null;

  if (active.kind === 'tab') {
    const from = tabs.findIndex((tab) => tab.key === active.key);
    const to = tabs.findIndex((tab) => tab.key === over.key);
    return arrayMove(tabs, from, to);
  }

  if (active.kind === 'section') {
    const target = tabs.findIndex((tab) =>
      over.kind === 'tab' ? tab.key === over.key : tab.sections.some((s) => s.key === over.key),
    );
    const lists = relocate(
      tabs.map((tab) => tab.sections),
      active.key,
      target,
      over.kind === 'section' ? over.key : undefined,
    );
    return tabs.map((tab, i) => ({ ...tab, sections: lists[i] ?? [] }));
  }

  const sections = tabs.flatMap((tab) => tab.sections);
  const target = sections.findIndex((section) =>
    over.kind === 'section'
      ? section.key === over.key
      : section.fields.some((field) => field.key === over.key),
  );
  const lists = relocate(
    sections.map((section) => section.fields),
    active.key,
    target,
    over.kind === 'field' ? over.key : undefined,
  );
  const fieldsOf = new Map(sections.map((section, i) => [section.key, lists[i] ?? []]));
  return tabs.map((tab) => ({
    ...tab,
    sections: tab.sections.map((section) => ({
      ...section,
      fields: fieldsOf.get(section.key) ?? [],
    })),
  }));
}

export const toLayoutRequest = (tabs: TabNode[]): LayoutRequest => ({
  tabs: tabs.map((tab) => ({
    key: tab.key,
    sections: tab.sections.map((section) => ({
      key: section.key,
      fields: section.fields.map((field) => field.key),
    })),
  })),
});
