import { UserOutlined } from '@ant-design/icons';
import { getGetCustomerQueryKey, useGetCustomer, useSetCustomersStatus } from '@erp/api-client';
import { DetailsPageLayout, ForbiddenResult } from '@erp/ui';
import { useTranslation } from 'react-i18next';
import { useParams } from 'react-router';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { useFeatureConfig } from '@/common/hooks/useFeatureConfig';
import { useUnitKey } from '@/common/master-data';
import { PartnerView } from '../../components/PartnerView';
import { CUSTOMER_FEATURE, customerPaths } from '../../partner-management';

export function CustomerViewPage() {
  const { t } = useTranslation();
  const id = Number(useParams().id);
  const access = useFeatureAccess(CUSTOMER_FEATURE);
  const config = useFeatureConfig(CUSTOMER_FEATURE, access.view);
  const query = useGetCustomer(id, {
    query: { queryKey: useUnitKey(getGetCustomerQueryKey(id)), enabled: access.view },
  });
  const setStatus = useSetCustomersStatus();

  if (!access.view) return <ForbiddenResult />;

  if (!query.data || !config.data) {
    const loading = query.isPending || config.isPending;
    return (
      <DetailsPageLayout
        loading={loading}
        missing={!loading}
        missingTitle={t('customer.notFound')}
        missingActions={[{ key: 'back', label: t('customer.backToList'), to: customerPaths.list }]}
      />
    );
  }

  return (
    <PartnerView
      partner={query.data}
      config={config.data}
      feature={CUSTOMER_FEATURE}
      paths={customerPaths}
      icon={<UserOutlined />}
      tint="blue"
      onSetStatus={(isActive) => setStatus.mutateAsync({ data: { ids: [id], isActive } })}
    />
  );
}
