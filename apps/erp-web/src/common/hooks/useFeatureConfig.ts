import {
  getGetFeatureConfigQueryKey,
  useGetErpNavigation,
  useGetFeatureConfig,
} from '@erp/api-client';
import { appConfig } from '@/common/config/app.config';

export function useFeatureConfig(feature: string, enabled = true) {
  const navigation = useGetErpNavigation({ query: { staleTime: appConfig.sessionStaleMs } });
  const version = navigation.data
    ?.flatMap((module) => module.subModules)
    .flatMap((subModule) => subModule.features)
    .find((f) => f.code === feature)?.configVersion;

  return useGetFeatureConfig(feature, {
    query: {
      queryKey: [...getGetFeatureConfigQueryKey(feature), version],
      enabled: enabled && version !== undefined,
      staleTime: Infinity,
    },
  });
}
