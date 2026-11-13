import type { FeatureConfigEditor, FieldConfigRow, SectionConfigRow } from '@erp/api-client';
import { buildLayout, moveNode, nodeId, toLayoutRequest } from './layout-model';

const section = (key: string, sortOrder: number, tabKey: string | null): SectionConfigRow => ({
  key,
  isTab: tabKey === null,
  base: { labelEn: key, sortOrder, tabKey, isVisible: true, columns: 4 },
  override: null,
});

const field = (key: string, sortOrder: number, sectionKey: string): FieldConfigRow =>
  ({
    key,
    type: 'text',
    isActive: true,
    base: { labelEn: key, sortOrder, sectionKey },
    override: null,
  }) as FieldConfigRow;

const editor = {
  feature: 'x.y',
  sections: [],
  customTypes: [],
  ownLayout: false,
  layoutUnitIds: [],
  layout: [
    section('prices', 20, 'general'),
    section('general', 10, null),
    section('basic', 10, 'general'),
    section('tax', 20, null),
    section('vat', 10, 'tax'),
  ],
  fields: [
    field('code', 20, 'basic'),
    field('name', 10, 'basic'),
    field('salesPrice', 10, 'prices'),
    { ...field('old', 30, 'basic'), isActive: false },
    { ...field('rate', 10, 'vat'), override: { sectionKey: 'prices', sortOrder: 20 } },
  ],
} as FeatureConfigEditor;

const shape = (tabs: ReturnType<typeof buildLayout>) => toLayoutRequest(tabs).tabs;

describe('layout model', () => {
  it('builds tabs, sections and active fields in their effective place and order', () => {
    expect(shape(buildLayout(editor))).toEqual([
      {
        key: 'general',
        sections: [
          { key: 'basic', fields: ['name', 'code'] },
          { key: 'prices', fields: ['salesPrice', 'rate'] },
        ],
      },
      { key: 'tax', sections: [{ key: 'vat', fields: [] }] },
    ]);
  });

  it('reorders a field inside its section and moves it to another one', () => {
    const tabs = buildLayout(editor);

    const swapped = moveNode(tabs, nodeId('field', 'code'), nodeId('field', 'name'));
    expect(shape(swapped!)[0]!.sections[0]!.fields).toEqual(['code', 'name']);

    const before = moveNode(tabs, nodeId('field', 'code'), nodeId('field', 'rate'));
    expect(shape(before!)[0]!.sections[1]!.fields).toEqual(['salesPrice', 'code', 'rate']);

    const appended = moveNode(tabs, nodeId('field', 'code'), nodeId('section', 'vat'));
    expect(shape(appended!)[1]!.sections[0]!.fields).toEqual(['code']);
  });

  it('moves sections between tabs and reorders tabs', () => {
    const tabs = buildLayout(editor);

    const moved = moveNode(tabs, nodeId('section', 'prices'), nodeId('tab', 'tax'));
    expect(shape(moved!).map((tab) => tab.sections.map((s) => s.key))).toEqual([
      ['basic'],
      ['vat', 'prices'],
    ]);

    const reordered = moveNode(tabs, nodeId('tab', 'tax'), nodeId('tab', 'general'));
    expect(shape(reordered!).map((tab) => tab.key)).toEqual(['tax', 'general']);
  });

  it('ignores drops that make no sense', () => {
    const tabs = buildLayout(editor);

    expect(moveNode(tabs, nodeId('tab', 'tax'), nodeId('section', 'basic'))).toBeNull();
    expect(moveNode(tabs, nodeId('field', 'code'), nodeId('tab', 'tax'))).toBeNull();
    expect(moveNode(tabs, nodeId('field', 'code'), nodeId('field', 'code'))).toBeNull();
  });
});
