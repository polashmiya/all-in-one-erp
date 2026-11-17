import { ShopOutlined } from '@ant-design/icons';
import { getGetSupplierQueryKey, useGetSupplier, useSetSuppliersStatus } from '@erp/api-client';
import { DetailsPageLayout, ForbiddenResult } from '@erp/ui';
import { useTranslation } from 'react-i18next';
import { useParams } from 'react-router';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { useFeatureConfig } from '@/common/hooks/useFeatureConfig';
import { useUnitKey } from '@/common/master-data';
import { PartnerView } from '../../components/PartnerView';
import { SUPPLIER_FEATURE, supplierPaths } from '../../partner-management';

export function SupplierViewPage() {
  const { t } = useTranslation();
  const id = Number(useParams().id);
  const access = useFeatureAccess(SUPPLIER_FEATURE);
  const config = useFeatureConfig(SUPPLIER_FEATURE, access.view);
  const query = useGetSupplier(id, {
    query: { queryKey: useUnitKey(getGetSupplierQueryKey(id)), enabled: access.view },
  });
  const setStatus = useSetSuppliersStatus();

  if (!access.view) return <ForbiddenResult />;

  if (!query.data || !config.data) {
    const loading = query.isPending || config.isPending;
    return (
      <DetailsPageLayout
        loading={loading}
        missing={!loading}
        missingTitle={t('supplier.notFound')}
        missingActions={[{ key: 'back', label: t('supplier.backToList'), to: supplierPaths.list }]}
      />
    );
  }

  return (
    <PartnerView
      partner={query.data}
      config={config.data}
      feature={SUPPLIER_FEATURE}
      paths={supplierPaths}
      icon={<ShopOutlined />}
      tint="amber"
      onSetStatus={(isActive) => setStatus.mutateAsync({ data: { ids: [id], isActive } })}
    />
  );
}
