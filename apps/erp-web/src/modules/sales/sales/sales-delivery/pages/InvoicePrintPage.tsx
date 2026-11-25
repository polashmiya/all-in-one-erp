import {
  getGetSalesDeliveryQueryKey,
  useGetSalesDelivery,
  type SalesDeliveryLineDetail,
} from '@erp/api-client';
import { ForbiddenResult, formatDate, formatMoney, formatNumber } from '@erp/ui';
import { useTranslation } from 'react-i18next';
import { useParams } from 'react-router';
import { QUANTITY_DECIMALS_SHOWN } from '@/common/documents/documents';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { useUnitKey } from '@/common/master-data';
import { SalesPrintPage } from '../../../print/SalesPrintPage';
import { PrintItem } from '../components/PrintItem';
import { DELIVERY_FEATURE, deliveryPaths } from '../constants';
import { dueDateOf } from '../delivery-form';

export function InvoicePrintPage() {
  const { t } = useTranslation();
  const id = Number(useParams().id);
  const access = useFeatureAccess(DELIVERY_FEATURE);
  const query = useGetSalesDelivery(id, {
    query: { queryKey: useUnitKey(getGetSalesDeliveryQueryKey(id)), enabled: access.view },
  });

  if (!access.view) return <ForbiddenResult />;

  const delivery = query.data;
  const amount = (value: number) => formatMoney(value, { symbol: false });
  const total = (label: string, value: number | undefined, sign = 1) =>
    value ? [{ label, value: amount(value * sign) }] : [];

  return (
    <SalesPrintPage<SalesDeliveryLineDetail>
      title={t('print.title.invoice')}
      backTo={deliveryPaths.view(id)}
      loading={query.isPending}
      record={delivery}
      notFound={t('salesDelivery.notFound')}
      partyTitle={t('print.billTo')}
      party={(customer) => [
        { label: t('print.customerCode'), value: customer.code },
        {
          label: t('print.contact'),
          value: delivery?.shippingContactPerson ?? customer.contactPerson,
        },
        { label: t('print.phone'), value: delivery?.phone ?? customer.phone },
        {
          label: t('print.address'),
          value: delivery?.anonymousAddress ?? customer.address,
        },
        { label: t('print.shipTo'), value: delivery?.shippingAddressName },
        { label: t('print.bin'), value: customer.binNumber },
        { label: t('print.tin'), value: customer.tinNumber },
      ]}
      facts={[
        { label: t('print.f.invoiceNo'), value: delivery?.code },
        { label: t('print.f.billNo'), value: delivery?.billNo },
        { label: t('print.f.date'), value: formatDate(delivery?.deliveryDate) },
        { label: t('print.f.orderNo'), value: delivery?.orderName },
        { label: t('print.f.customerPo'), value: delivery?.customerPo },
        { label: t('print.f.challanNo'), value: delivery?.challanNo },
        { label: t('print.f.vatChallanNo'), value: delivery?.vatChallanNo },
        { label: t('print.f.paymentType'), value: delivery?.paymentTypeName },
        { label: t('print.f.dueDate'), value: delivery && dueDateOf(delivery) },
        { label: t('print.f.salesForce'), value: delivery?.salesForceName },
      ]}
      columns={[
        {
          key: 'item',
          title: t('print.item'),
          render: (line) => <PrintItem line={line} />,
        },
        {
          key: 'quantity',
          title: t('document.lines.quantity'),
          end: true,
          render: (line) =>
            `${formatNumber(line.quantity, QUANTITY_DECIMALS_SHOWN)} ${line.uomName}`,
        },
        {
          key: 'price',
          title: t('document.lines.price'),
          end: true,
          render: (line) => (line.isOfferItem ? t('sales.lines.free') : amount(line.price)),
        },
        {
          key: 'discount',
          title: t('document.lines.discount'),
          end: true,
          render: (line) => amount(line.discountAmount),
        },
        {
          key: 'sd',
          title: t('sales.lines.sd'),
          end: true,
          render: (line) => amount(line.sdAmount),
        },
        {
          key: 'vat',
          title: t('document.lines.vat'),
          end: true,
          render: (line) => amount(line.vatAmount),
        },
        {
          key: 'amount',
          title: t('sales.lines.amount'),
          end: true,
          render: (line) => amount(line.totalValue),
        },
      ]}
      lines={delivery?.lines ?? []}
      totals={
        delivery
          ? [
              { label: t('sales.totals.gross'), value: amount(delivery.itemTotalAmount) },
              ...total(
                t('document.totals.discount'),
                delivery.headerDiscount + delivery.totalLineDiscount,
                -1,
              ),
              ...total(t('sales.totals.sd'), delivery.totalSd),
              ...total(t('document.totals.vat'), delivery.totalVat),
              ...total(t('sales.totals.ait'), delivery.totalAit),
              ...total(t('document.totals.charges'), delivery.othersCost + delivery.extraCharge),
              ...total(
                t('sales.totals.reductions'),
                delivery.specialDiscountAmount + delivery.deductionValue,
                -1,
              ),
              {
                label: t('sales.totals.total'),
                value: formatMoney(delivery.netAmount),
                strong: true,
              },
              ...total(t('salesDelivery.advanceUsed'), delivery.advanceBalanceAdjust, -1),
              ...total(
                t('salesDelivery.collected'),
                delivery.receiveAmount + delivery.collectedAmount,
                -1,
              ),
              ...total(t('salesDelivery.returnAdjusted'), delivery.returnAdjusted, -1),
              {
                label: t('salesDelivery.due'),
                value: formatMoney(delivery.pendingAmount),
                strong: true,
              },
            ]
          : []
      }
      amount={delivery?.netAmount}
      notes={[delivery?.isVatInclusive && t('print.vatInclusive'), delivery?.remarks]}
      signatures={[t('print.sign.customer'), t('print.sign.prepared'), t('print.sign.authorised')]}
    />
  );
}
