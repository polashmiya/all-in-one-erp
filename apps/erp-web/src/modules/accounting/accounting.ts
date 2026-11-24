import { getGetChartOfAccountOptionsQueryKey, useGetChartOfAccountOptions } from '@erp/api-client';
import type { LookupItem } from '@erp/metadata-engine';
import { useTranslation } from 'react-i18next';
import { useUnitKey } from '@/common/master-data';

export const ACCOUNTING_MODULE = 'accounting';

export const ACCOUNT_TYPES = ['asset', 'liability', 'equity', 'income', 'expense'];

export function useAccountTypes(): LookupItem[] {
  const { t } = useTranslation();
  return ACCOUNT_TYPES.map((id) => ({ id, name: t(`accounting.type.${id}`), isActive: true }));
}

export function useAccountOptions(enabled = true) {
  const query = useGetChartOfAccountOptions({
    query: { queryKey: useUnitKey(getGetChartOfAccountOptionsQueryKey()), enabled },
  });
  return query.data;
}
