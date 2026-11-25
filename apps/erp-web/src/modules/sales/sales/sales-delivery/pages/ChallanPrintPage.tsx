import {
  getGetSalesDeliveryQueryKey,
  useGetSalesDelivery,
  type SalesDeliveryLineDetail,
} from '@erp/api-client';
import { ForbiddenResult, formatDate, formatNumber } from '@erp/ui';
import { useTranslation } from 'react-i18next';
import { useParams } from 'react-router';
import { QUANTITY_DECIMALS_SHOWN } from '@/common/documents/documents';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { useUnitKey } from '@/common/master-data';
import { SalesPrintPage } from '../../../print/SalesPrintPage';
import { PrintItem } from '../components/PrintItem';
import { DELIVERY_FEATURE, deliveryPaths } from '../constants';

export function ChallanPrintPage() {
  const { t } = useTranslation();
  const id = Number(useParams().id);
  const access = useFeatureAccess(DELIVERY_FEATURE);
  const query = useGetSalesDelivery(id, {
    query: { queryKey: useUnitKey(getGetSalesDeliveryQueryKey(id)), enabled: access.view },
  });

  if (!access.view) return <ForbiddenResult />;

  const delivery = query.data;

  return (
    <SalesPrintPage<SalesDeliveryLineDetail>
      title={t('print.title.challan')}
      backTo={deliveryPaths.view(id)}
      loading={query.isPending}
      record={delivery}
      notFound={t('salesDelivery.notFound')}
      partyTitle={t('print.deliverTo')}
      party={(customer) => [
        { label: t('print.customerCode'), value: customer.code },
        {
          label: t('print.contact'),
          value: delivery?.shippingContactPerson ?? customer.contactPerson,
        },
        { label: t('print.phone'), value: delivery?.phone ?? customer.phone },
        {
          label: t('print.address'),
          value: delivery?.shippingAddressName ?? delivery?.anonymousAddress ?? customer.address,
        },
      ]}
      facts={[
        { label: t('print.f.challanNo'), value: delivery?.challanNo ?? delivery?.code },
        { label: t('print.f.deliveryNo'), value: delivery?.challanNo && delivery.code },
        { label: t('print.f.date'), value: formatDate(delivery?.deliveryDate) },
        { label: t('print.f.orderNo'), value: delivery?.orderName },
        { label: t('print.f.customerPo'), value: delivery?.customerPo },
        { label: t('print.f.vatChallanNo'), value: delivery?.vatChallanNo },
        { label: t('print.f.warehouse'), value: delivery?.warehouseName },
        { label: t('print.f.deliveryMethod'), value: delivery?.deliveryMethodName },
        { label: t('print.f.courier'), value: delivery?.courierOrEmployeeName },
      ]}
      columns={[
        {
          key: 'item',
          title: t('print.item'),
          render: (line) => <PrintItem line={line} />,
        },
        { key: 'pack', title: t('salesDelivery.lines.pack'), render: (line) => line.packQuantity },
        { key: 'uom', title: t('document.lines.uom'), render: (line) => line.uomName },
        {
          key: 'quantity',
          title: t('document.lines.quantity'),
          end: true,
          render: (line) => formatNumber(line.quantity, QUANTITY_DECIMALS_SHOWN),
        },
      ]}
      lines={delivery?.lines ?? []}
      totals={[
        {
          label: t('document.lines.totalQuantity'),
          value: formatNumber(delivery?.totalQuantity, QUANTITY_DECIMALS_SHOWN),
          strong: true,
        },
      ]}
      notes={[delivery?.remarks]}
      signatures={[t('print.sign.receiver'), t('print.sign.delivered'), t('print.sign.authorised')]}
    />
  );
}
