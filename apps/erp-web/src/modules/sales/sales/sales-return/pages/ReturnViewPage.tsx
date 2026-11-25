import {
  CalendarOutlined,
  CarOutlined,
  HomeOutlined,
  IdcardOutlined,
  RollbackOutlined,
  UserOutlined,
} from '@ant-design/icons';
import {
  getGetSalesReturnQueryKey,
  useGetSalesReturn,
  type SalesReturnDetail,
  type SalesReturnLineDetail,
} from '@erp/api-client';
import {
  DescriptionList,
  DetailSection,
  DetailsPageLayout,
  ForbiddenResult,
  formatDate,
  formatMoney,
  formatNumber,
  formatPercent,
  StatusTag,
} from '@erp/ui';
import { useTranslation } from 'react-i18next';
import { Link, useParams } from 'react-router';
import { DocumentView } from '@/common/documents/DocumentView';
import { QUANTITY_DECIMALS_SHOWN } from '@/common/documents/documents';
import { LinesView } from '@/common/documents/LinesView';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { useFeatureConfig } from '@/common/hooks/useFeatureConfig';
import { useUnitKey } from '@/common/master-data';
import { deliveryPaths } from '../../sales-delivery/constants';
import { RETURN_FEATURE, returnPaths } from '../constants';
import { returnCalls } from '../return-form';

export function ReturnViewPage() {
  const { t } = useTranslation();
  const id = Number(useParams().id);
  const access = useFeatureAccess(RETURN_FEATURE);
  const config = useFeatureConfig(RETURN_FEATURE, access.view);
  const query = useGetSalesReturn(id, {
    query: { queryKey: useUnitKey(getGetSalesReturnQueryKey(id)), enabled: access.view },
  });

  if (!access.view) return <ForbiddenResult />;

  const record = query.data;
  if (!record || !config.data) {
    const loading = query.isPending || config.isPending;
    return (
      <DetailsPageLayout
        loading={loading}
        missing={!loading}
        missingTitle={t('salesReturn.notFound')}
        missingActions={[{ key: 'back', label: t('salesReturn.backToList'), to: returnPaths.list }]}
      />
    );
  }

  const money = (key: string, label: string, value: number) => ({
    key,
    label,
    value: formatMoney(value),
  });
  const tax = (percentage: number, amount: number) =>
    `${formatPercent(percentage)} · ${formatMoney(amount)}`;

  return (
    <DocumentView<SalesReturnDetail>
      document={record}
      config={config.data}
      feature={RETURN_FEATURE}
      paths={returnPaths}
      calls={returnCalls}
      icon={<RollbackOutlined />}
      tag={record.returnTypeName}
      meta={[
        { key: 'customer', icon: <UserOutlined />, label: record.customerName },
        {
          key: 'delivery',
          icon: <CarOutlined />,
          label: record.deliveryId ? (
            <Link to={deliveryPaths.view(record.deliveryId)}>{record.deliveryName}</Link>
          ) : (
            t(`salesReturn.source.${record.source}`)
          ),
        },
        {
          key: 'date',
          icon: <CalendarOutlined />,
          label: t('salesReturn.meta.returned', { date: formatDate(record.returnDate) }),
        },
        { key: 'warehouse', icon: <HomeOutlined />, label: record.warehouseName },
        { key: 'receivedBy', icon: <IdcardOutlined />, label: record.receivedByName },
      ]}
      renderers={{ source: (r) => t(`salesReturn.source.${r.source}`) }}
      sections={[
        {
          key: 'settled',
          content: (
            <DetailSection title={t('salesReturn.settlement')}>
              <DescriptionList
                columns={3}
                items={[
                  money('due', t('salesReturn.dueAdjusted'), record.dueAdjusted),
                  money('refund', t('salesReturn.refund'), record.refundAmount),
                  money('advance', t('salesReturn.advance'), record.advanceAmount),
                  money('cost', t('salesReturn.cost'), record.totalCost - record.lossAmount),
                  money('loss', t('salesReturn.loss'), record.lossAmount),
                ]}
              />
            </DetailSection>
          ),
        },
      ]}
      lines={
        <LinesView<SalesReturnLineDetail>
          rows={record.lines}
          columns={[
            ...(record.deliveryId
              ? [
                  {
                    key: 'delivered',
                    title: t('salesReturn.lines.delivered'),
                    align: 'end' as const,
                    render: (_: unknown, line: SalesReturnLineDetail) =>
                      formatNumber(line.delivered, QUANTITY_DECIMALS_SHOWN),
                  },
                ]
              : []),
            {
              key: 'price',
              title: t('document.lines.price'),
              align: 'end',
              render: (_, line) =>
                line.isOfferItem ? (
                  <StatusTag tone="success">{t('sales.lines.freeItem')}</StatusTag>
                ) : (
                  formatMoney(line.price)
                ),
            },
            {
              key: 'discount',
              title: t('document.lines.discount'),
              align: 'end',
              render: (_, line) => formatMoney(line.discountAmount),
            },
            {
              key: 'amount',
              title: t('sales.lines.amount'),
              align: 'end',
              render: (_, line) => formatMoney(line.totalValue),
            },
          ]}
          details={(line) => (
            <DescriptionList
              columns={4}
              items={[
                {
                  key: 'sd',
                  label: t('sales.lines.sd'),
                  value: tax(line.sdPercentage, line.sdAmount),
                },
                {
                  key: 'vat',
                  label: t('document.lines.vat'),
                  value: tax(line.vatPercentage, line.vatAmount),
                },
                {
                  key: 'ait',
                  label: t('sales.lines.ait'),
                  value: tax(line.aitPercentage, line.aitAmount),
                },
                money('cost', t('salesReturn.lines.cost'), line.costTotal),
                {
                  key: 'damaged',
                  label: t('salesReturn.lines.loss'),
                  value: line.isDamaged ? (
                    <StatusTag tone="danger">{formatMoney(line.lossAmount)}</StatusTag>
                  ) : null,
                },
                { key: 'batch', label: t('salesReturn.lines.batch'), value: line.batch },
                { key: 'serial', label: t('salesReturn.lines.serial'), value: line.itemSerial },
                { key: 'remarks', label: t('salesReturn.lines.remarks'), value: line.remarks },
              ]}
            />
          )}
          totals={[
            {
              key: 'quantity',
              label: t('document.lines.totalQuantity'),
              value: formatNumber(record.totalQuantity, QUANTITY_DECIMALS_SHOWN),
            },
            money('gross', t('sales.totals.gross'), record.itemTotalAmount),
            money('discount', t('document.totals.discount'), record.totalDiscount),
            money('sd', t('sales.totals.sd'), record.totalSd),
            money('vat', t('document.totals.vat'), record.totalVat),
            money('ait', t('sales.totals.ait'), record.totalAit),
            money('charges', t('document.totals.charges'), record.chargeRefundAmount),
            money(
              'reductions',
              t('sales.totals.reductions'),
              record.specialDiscountAmount + record.deductionValue,
            ),
            { ...money('total', t('sales.totals.total'), record.netAmount), strong: true },
          ]}
        />
      }
    />
  );
}
