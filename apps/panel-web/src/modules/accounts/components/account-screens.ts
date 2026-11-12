import { useGetAccount } from '@erp/api-client';
import { useText } from '@erp/metadata-engine';
import { useFeatureCatalog } from './feature-catalog';

export function useAccountScreens(accountId: string) {
  const text = useText();
  const account = useGetAccount(accountId);
  const { modules } = useFeatureCatalog();

  const granted = account.data?.featureCodes ?? [];
  const features = modules.flatMap((module) =>
    module.subModules.flatMap((subModule) =>
      subModule.features
        .filter((feature) => granted.includes(feature.code))
        .map((feature) => ({
          value: feature.code,
          label: `${text(subModule.label)} › ${text(feature.label)}`,
        })),
    ),
  );
  const units = (account.data?.businessUnits ?? []).map((unit) => ({
    value: unit.id,
    label: unit.name,
  }));

  return { account, features, units };
}
