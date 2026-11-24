import {
  ApiError,
  getGetAccountFeaturesQueryKey,
  getGetAccountQueryKey,
  getListAccountsQueryKey,
  useUpdateAccountFeatures,
} from '@erp/api-client';
import { useQueryClient } from '@tanstack/react-query';
import { App } from 'antd';
import { useTranslation } from 'react-i18next';

export function useSaveFeatures(accountId: string) {
  const { t } = useTranslation();
  const { message } = App.useApp();
  const queryClient = useQueryClient();
  const update = useUpdateAccountFeatures();

  const save = async (featureCodes: string[]) => {
    try {
      await update.mutateAsync({ id: accountId, data: { featureCodes } });
    } catch (error) {
      void message.error(
        error instanceof ApiError
          ? (Object.values(error.fieldErrors)[0]?.[0] ?? error.message)
          : t('menus.saveFailed'),
      );
      throw error;
    }
    void message.success(t('menus.saved'));
    await Promise.all(
      [
        getGetAccountQueryKey(accountId),
        getGetAccountFeaturesQueryKey(accountId),
        getListAccountsQueryKey(),
      ].map((queryKey) => queryClient.invalidateQueries({ queryKey })),
    );
  };

  return { save, saving: update.isPending };
}
