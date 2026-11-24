import { useGetErpMenuCatalog, type ErpMenuModule, type LocalizedLabel } from '@erp/api-client';
import { useMemo } from 'react';

export function useFeatureCatalog() {
  const query = useGetErpMenuCatalog({ query: { staleTime: 10 * 60_000 } });
  const modules = useMemo<ErpMenuModule[]>(() => query.data ?? [], [query.data]);
  const allCodes = useMemo(
    () => modules.flatMap((m) => m.subModules.flatMap((s) => s.features.map((f) => f.code))),
    [modules],
  );
  return { modules, allCodes, loading: query.isPending };
}

export function searchCatalog(
  modules: ErpMenuModule[],
  query: string,
  label: (text: LocalizedLabel) => string,
) {
  const term = query.trim().toLowerCase();
  const codes = modules.flatMap((module) =>
    module.subModules.flatMap((sub) =>
      sub.features
        .filter((feature) =>
          [label(module.label), label(sub.label), label(feature.label), feature.code].some((text) =>
            text.toLowerCase().includes(term),
          ),
        )
        .map((feature) => feature.code),
    ),
  );
  return limitCatalog(modules, codes);
}

export function limitCatalog(modules: ErpMenuModule[], available?: string[]) {
  const limited = available
    ? modules
        .map((module) => ({
          ...module,
          subModules: module.subModules
            .map((sub) => ({
              ...sub,
              features: sub.features.filter((f) => available.includes(f.code)),
            }))
            .filter((sub) => sub.features.length > 0),
        }))
        .filter((module) => module.subModules.length > 0)
    : modules;
  return {
    modules: limited,
    allCodes: limited.flatMap((m) => m.subModules.flatMap((s) => s.features.map((f) => f.code))),
  };
}
