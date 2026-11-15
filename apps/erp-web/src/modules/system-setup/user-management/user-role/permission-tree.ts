import type { LocalizedLabel, PermissionTreeFeature, PermissionTreeModule } from '@erp/api-client';
import { STANDARD_ACTIONS, VIEW_ACTION } from './constants';

export type CheckState = 'on' | 'mix' | 'off';

export const permissionKey = (feature: PermissionTreeFeature, action: string) =>
  `${feature.code}.${action}`;

export const keysOf = (features: PermissionTreeFeature[]) =>
  features.flatMap((feature) => feature.actions.map((action) => permissionKey(feature, action)));

export const otherActions = (feature: PermissionTreeFeature) =>
  feature.actions.filter((action) => !(STANDARD_ACTIONS as readonly string[]).includes(action));

export const featuresOf = (modules: PermissionTreeModule[]) =>
  modules.flatMap((module) => module.subModules.flatMap((sub) => sub.features));

export function checkState(selected: ReadonlySet<string>, keys: string[]): CheckState {
  const count = keys.filter((key) => selected.has(key)).length;
  if (count === 0) return 'off';
  return count === keys.length ? 'on' : 'mix';
}

export function setAll(
  selected: ReadonlySet<string>,
  features: PermissionTreeFeature[],
  value: boolean,
) {
  const next = new Set(selected);
  for (const key of keysOf(features)) {
    if (value) next.add(key);
    else next.delete(key);
  }
  return next;
}

export function setAction(
  selected: ReadonlySet<string>,
  features: PermissionTreeFeature[],
  action: string,
  value: boolean,
) {
  const next = new Set(selected);
  for (const feature of features.filter((f) => f.actions.includes(action))) {
    if (value) {
      next.add(permissionKey(feature, action));
      next.add(permissionKey(feature, VIEW_ACTION));
    } else if (action === VIEW_ACTION) {
      for (const key of keysOf([feature])) next.delete(key);
    } else {
      next.delete(permissionKey(feature, action));
    }
  }
  return next;
}

export function filterTree(
  modules: PermissionTreeModule[],
  keep: (feature: PermissionTreeFeature, words: string[]) => boolean,
  label: (text: LocalizedLabel) => string,
) {
  return modules
    .map((module) => ({
      ...module,
      subModules: module.subModules
        .map((sub) => ({
          ...sub,
          features: sub.features.filter((feature) =>
            keep(feature, [label(module.name), label(sub.name), label(feature.name)]),
          ),
        }))
        .filter((sub) => sub.features.length > 0),
    }))
    .filter((module) => module.subModules.length > 0);
}
