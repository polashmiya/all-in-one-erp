import { DashboardOutlined } from '@ant-design/icons';
import { useGetErpNavigation, type ErpMenuModule, type LocalizedLabel } from '@erp/api-client';
import { resolveIcon, type LocalizedText, type MenuNode } from '@erp/ui';
import { useMemo } from 'react';
import { appConfig } from '@/common/config/app.config';

const dashboard: MenuNode = {
  key: 'dashboard',
  label: { en: 'Dashboard', bn: 'ড্যাশবোর্ড' },
  icon: <DashboardOutlined />,
  path: '/dashboard',
};

const text = (label: LocalizedLabel): LocalizedText => ({
  en: label.en,
  ...(label.bn ? { bn: label.bn } : {}),
});

export function toMenuNodes(modules: ErpMenuModule[]): MenuNode[] {
  return modules.map((module) => ({
    key: module.key,
    label: text(module.label),
    icon: resolveIcon(module.icon),
    children: module.subModules.map((subModule) => ({
      key: `${module.key}.${subModule.key}`,
      label: text(subModule.label),
      children: subModule.features.map((feature) => ({
        key: feature.code,
        label: text(feature.label),
        path: feature.path,
      })),
    })),
  }));
}

export function useErpNavigation() {
  const query = useGetErpNavigation({
    query: { staleTime: appConfig.sessionStaleMs, refetchInterval: appConfig.sessionStaleMs },
  });
  const modules = useMemo(() => toMenuNodes(query.data ?? []), [query.data]);
  const menu = useMemo(() => [dashboard, ...modules], [modules]);

  return { menu, modules, loading: query.isPending };
}
