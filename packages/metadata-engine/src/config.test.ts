import type { ConfigField, FeatureConfig } from '@erp/api-client';
import { defaultValues, displayKey, fieldsOf, namePath } from './config';
import { childSelectOptions, localized, selectOptions, type LookupItem } from './lookups';

const field = (key: string, type: string, defaultValue: string | null = null): ConfigField => ({
  key,
  type,
  label: { en: key, bn: null },
  help: null,
  placeholder: null,
  required: false,
  readOnly: false,
  colSpan: 1,
  defaultValue,
  maxLength: null,
  min: null,
  max: null,
  precision: null,
  lookup: type === 'lookup' ? 'source' : null,
  parentField: null,
});

const config: FeatureConfig = {
  feature: 'x.y',
  version: 1,
  columns: [],
  filters: [],
  tabs: [
    {
      key: 'main',
      label: { en: 'Main', bn: null },
      sections: [
        {
          key: 'info',
          label: { en: 'Info', bn: null },
          columns: 4,
          fields: [
            field('active', 'boolean', 'true'),
            field('isChild', 'boolean'),
            field('typeId', 'lookup', '3'),
            field('vat', 'percent', '15'),
            field('note', 'text', 'hello'),
            field('name', 'text'),
          ],
        },
      ],
    },
  ],
};

const item = (id: number, parentId: number | null, isActive = true): LookupItem => ({
  id,
  code: null,
  name: `N${id}`,
  isActive,
  parentId,
});

describe('metadata-engine config', () => {
  it('builds typed default values and false for booleans without a default', () => {
    expect(defaultValues(config)).toEqual({
      active: true,
      isChild: false,
      typeId: 3,
      vat: 15,
      note: 'hello',
    });
  });

  it('keeps custom fields under customData', () => {
    const warranty = { ...field('warranty', 'number', '12'), custom: true };
    const section = {
      key: 'more',
      label: { en: 'More', bn: null },
      columns: 4,
      fields: [warranty],
    };
    const custom = { ...config, tabs: [{ ...config.tabs[0]!, sections: [section] }] };

    expect(defaultValues(custom)).toEqual({ customData: { warranty: 12 } });
    expect(namePath(warranty)).toEqual(['customData', 'warranty']);
    expect(namePath(field('name', 'text'))).toBe('name');
  });

  it('flattens fields and reads lookup names from the matching Name property', () => {
    expect(fieldsOf(config).map((f) => f.key)).toEqual([
      'active',
      'isChild',
      'typeId',
      'vat',
      'note',
      'name',
    ]);
    expect(displayKey(field('typeId', 'lookup'))).toBe('typeName');
    expect(displayKey(field('code', 'text'))).toBe('code');
  });

  it('keeps inactive options only when they are the current value', () => {
    const items = [item(1, null), item(2, null, false)];
    expect(selectOptions(items).map((o) => o.value)).toEqual([1]);
    expect(selectOptions(items, 2).map((o) => o.value)).toEqual([1, 2]);
  });

  it('groups child options by parent until a parent is chosen', () => {
    const parents = [item(10, null), item(20, null)];
    const children = [item(1, 10), item(2, 20), item(3, 20)];

    expect(childSelectOptions(children, parents, null)).toEqual([
      { label: 'N10', title: 'N10', options: [{ value: 1, label: 'N1' }] },
      {
        label: 'N20',
        title: 'N20',
        options: [
          { value: 2, label: 'N2' },
          { value: 3, label: 'N3' },
        ],
      },
    ]);
    expect(childSelectOptions(children, parents, 20)).toEqual([
      { value: 2, label: 'N2' },
      { value: 3, label: 'N3' },
    ]);
  });
});

describe('metadata-engine lookups', () => {
  it('shows Bangla names in Bangla and falls back to English', () => {
    const items = [
      { ...item(1, null), nameBn: 'এক' },
      { ...item(2, null), nameBn: null },
    ];

    expect(localized(items, 'bn')?.map((i) => i.name)).toEqual(['এক', 'N2']);
    expect(localized(items, 'en')?.map((i) => i.name)).toEqual(['N1', 'N2']);
  });
});
