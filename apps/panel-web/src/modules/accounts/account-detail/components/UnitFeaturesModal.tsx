import {
  getGetUnitFeaturesQueryKey,
  useGetUnitFeatures,
  useSaveUnitFeatures,
} from '@erp/api-client';
import { AppModal } from '@erp/ui';
import { useQueryClient } from '@tanstack/react-query';
import { Alert, App, Skeleton } from 'antd';
import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { FeatureAccessTree } from '../../components/FeatureAccess';

export interface UnitTarget {
  id: string;
  name: string;
}

export function UnitFeaturesModal({
  accountId,
  accountCodes,
  unit,
  onClose,
}: {
  accountId: string;
  accountCodes: string[];
  unit?: UnitTarget;
  onClose: () => void;
}) {
  const { t } = useTranslation();
  const { message } = App.useApp();
  const queryClient = useQueryClient();
  const unitId = unit?.id ?? '';
  const query = useGetUnitFeatures(accountId, unitId, { query: { enabled: !!unit } });
  const save = useSaveUnitFeatures();
  const [codes, setCodes] = useState<string[]>([]);

  useEffect(() => {
    if (query.data)
      setCodes(accountCodes.filter((code) => !query.data.disabledFeatureCodes.includes(code)));
  }, [query.data, accountCodes]);

  const submit = async () => {
    try {
      await save.mutateAsync({
        id: accountId,
        unitId,
        data: { disabledFeatureCodes: accountCodes.filter((code) => !codes.includes(code)) },
      });
      await queryClient.invalidateQueries({
        queryKey: getGetUnitFeaturesQueryKey(accountId, unitId),
      });
      void message.success(t('unitMenus.saved'));
      onClose();
    } catch {
      void message.error(t('menus.saveFailed'));
    }
  };

  return (
    <AppModal
      open={!!unit}
      onClose={onClose}
      size="lg"
      title={t('unitMenus.editTitle', { unit: unit?.name ?? '' })}
      okText={t('menus.save')}
      okLoading={save.isPending}
      onOk={submit}
    >
      <div className="erp-stack">
        <Alert type="info" showIcon message={t('unitMenus.note')} />
        {query.isPending ? (
          <Skeleton active paragraph={{ rows: 5 }} title={false} />
        ) : (
          <FeatureAccessTree value={codes} onChange={setCodes} available={accountCodes} />
        )}
      </div>
    </AppModal>
  );
}
