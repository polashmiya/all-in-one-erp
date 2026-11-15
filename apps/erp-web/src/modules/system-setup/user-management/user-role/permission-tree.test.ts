import type { PermissionTreeFeature, PermissionTreeModule } from '@erp/api-client';
import { checkState, filterTree, keysOf, otherActions, setAction, setAll } from './permission-tree';

const items: PermissionTreeFeature = {
  code: 'system-setup.item-profile',
  name: { en: 'Item Profile', bn: null },
  actions: ['view', 'create', 'edit'],
};
const pos: PermissionTreeFeature = {
  code: 'sales.pos',
  name: { en: 'POS', bn: null },
  actions: ['view', 'create', 'reprint'],
};
const modules: PermissionTreeModule[] = [
  {
    key: 'system-setup',
    name: { en: 'System Setup', bn: null },
    icon: null,
    subModules: [
      { key: 'items', name: { en: 'Item Management', bn: null }, icon: null, features: [items] },
    ],
  },
  {
    key: 'sales',
    name: { en: 'Sales', bn: null },
    icon: null,
    subModules: [{ key: 'pos', name: { en: 'POS', bn: null }, icon: null, features: [pos] }],
  },
];

describe('permission tree', () => {
  it('ticking an action ticks view too', () => {
    const next = setAction(new Set(), [items], 'edit', true);

    expect([...next].sort()).toEqual([
      'system-setup.item-profile.edit',
      'system-setup.item-profile.view',
    ]);
  });

  it('clearing view clears the whole screen', () => {
    const all = setAll(new Set(), [pos], true);

    expect(setAction(all, [pos], 'view', false).size).toBe(0);
  });

  it('reports on, mixed and off for a group', () => {
    const selected = setAction(new Set(), [items], 'view', true);

    expect(checkState(selected, keysOf([items]))).toBe('mix');
    expect(checkState(setAll(selected, [items], true), keysOf([items]))).toBe('on');
    expect(checkState(new Set(), keysOf([items]))).toBe('off');
  });

  it('a column only touches screens that have the action', () => {
    const next = setAction(new Set(), [items, pos], 'reprint', true);

    expect([...next].sort()).toEqual(['sales.pos.reprint', 'sales.pos.view']);
    expect(otherActions(pos)).toEqual(['reprint']);
  });

  it('filters by module, screen or sub-module name and drops empty branches', () => {
    const result = filterTree(
      modules,
      (_, names) => names.some((name) => name.toLowerCase().includes('item')),
      (text) => text.en,
    );

    expect(result.map((m) => m.key)).toEqual(['system-setup']);
  });
});
