import {
  CalendarOutlined,
  CopyOutlined,
  FileTextOutlined,
  HomeOutlined,
  IdcardOutlined,
  ShoppingCartOutlined,
  UserOutlined,
} from '@ant-design/icons';
import {
  getGetSalesQuotationQueryKey,
  problemMessage,
  useGetSalesQuotation,
  useReviseSalesQuotation,
  type SalesQuotationDetail,
  type SalesQuotationLineDetail,
} from '@erp/api-client';
import {
  DescriptionList,
  DetailsPageLayout,
  ForbiddenResult,
  formatDate,
  formatMoney,
  formatNumber,
  formatPercent,
} from '@erp/ui';
import { App } from 'antd';
import { useTranslation } from 'react-i18next';
import { useNavigate, useParams } from 'react-router';
import { DocumentView } from '@/common/documents/DocumentView';
import { QUANTITY_DECIMALS_SHOWN, useRefreshDocuments } from '@/common/documents/documents';
import { LinesView } from '@/common/documents/LinesView';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { useFeatureConfig } from '@/common/hooks/useFeatureConfig';
import { useUnitKey } from '@/common/master-data';
import { ORDER_FEATURE, orderPaths } from '../../sales-order/constants';
import { VersionHistory } from '../components/VersionHistory';
import { QUOTATION_FEATURE, quotationPaths } from '../constants';
import { quotationCalls } from '../quotation-form';

export function QuotationViewPage() {
  const { t } = useTranslation();
  const { message } = App.useApp();
  const navigate = useNavigate();
  const id = Number(useParams().id);
  const access = useFeatureAccess(QUOTATION_FEATURE);
  const orders = useFeatureAccess(ORDER_FEATURE);
  const refresh = useRefreshDocuments();
  const revise = useReviseSalesQuotation();
  const config = useFeatureConfig(QUOTATION_FEATURE, access.view);
  const query = useGetSalesQuotation(id, {
    query: { queryKey: useUnitKey(getGetSalesQuotationQueryKey(id)), enabled: access.view },
  });

  if (!access.view) return <ForbiddenResult />;

  const quotation = query.data;
  if (!quotation || !config.data) {
    const loading = query.isPending || config.isPending;
    return (
      <DetailsPageLayout
        loading={loading}
        missing={!loading}
        missingTitle={t('quotation.notFound')}
        missingActions={[
          { key: 'back', label: t('quotation.backToList'), to: quotationPaths.list },
        ]}
      />
    );
  }

  const makeVersion = async () => {
    try {
      const next = await revise.mutateAsync({ id });
      await refresh();
      void message.success(t('quotation.revised', { code: next.code, version: next.version }));
      void navigate(quotationPaths.view(next.id));
    } catch (error) {
      void message.error(problemMessage(error, t('master.failed')));
    }
  };

  const percentAndAmount = (percentage: number, amount: number) =>
    `${formatPercent(percentage)} · ${formatMoney(amount)}`;

  return (
    <DocumentView<SalesQuotationDetail>
      document={quotation}
      config={config.data}
      feature={QUOTATION_FEATURE}
      paths={quotationPaths}
      calls={quotationCalls}
      icon={<FileTextOutlined />}
      tag={[quotation.version, quotation.quotationTypeName].filter(Boolean).join(' · ')}
      meta={[
        { key: 'customer', icon: <UserOutlined />, label: quotation.customerName },
        {
          key: 'dates',
          icon: <CalendarOutlined />,
          label: [
            t('quotation.meta.quoted', { date: formatDate(quotation.quotationDate) }),
            t('quotation.meta.valid', { date: formatDate(quotation.validityDate) }),
            quotation.isExpired && t('quotation.expired'),
          ]
            .filter(Boolean)
            .join(' · '),
        },
        { key: 'office', icon: <HomeOutlined />, label: quotation.officeName },
        { key: 'salesForce', icon: <IdcardOutlined />, label: quotation.salesForceName },
      ]}
      renderers={{ shippingAddressId: (record) => record.shipToName ?? '—' }}
      moreActions={[
        {
          key: 'order',
          label: t('quotation.order'),
          icon: <ShoppingCartOutlined />,
          hidden:
            !orders.create ||
            quotation.status !== 'approved' ||
            quotation.isOrdered ||
            quotation.isExpired,
          onClick: () => navigate(orderPaths.fromQuotation(quotation.id)),
        },
        {
          key: 'revise',
          label: t('quotation.revise'),
          icon: <CopyOutlined />,
          hidden: !access.create || !quotation.canRevise,
          confirm: {
            title: t('quotation.reviseConfirm', { code: quotation.code }),
            description: t('quotation.reviseHint'),
            okText: t('quotation.revise'),
          },
          onClick: makeVersion,
        },
      ]}
      sections={
        quotation.versions.length > 1
          ? [
              {
                key: 'versions',
                content: <VersionHistory versions={quotation.versions} currentId={quotation.id} />,
              },
            ]
          : []
      }
      lines={
        <LinesView<SalesQuotationLineDetail>
          rows={quotation.lines}
          columns={[
            {
              key: 'price',
              title: t('document.lines.price'),
              align: 'end',
              render: (_, line) => formatMoney(line.price),
            },
            {
              key: 'discount',
              title: t('document.lines.discount'),
              align: 'end',
              render: (_, line) =>
                line.discountPercentage
                  ? percentAndAmount(line.discountPercentage, line.discountAmount)
                  : formatMoney(line.discountAmount),
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
              items={[
                {
                  key: 'sd',
                  label: t('sales.lines.sd'),
                  value: percentAndAmount(line.sdPercentage, line.sdAmount),
                },
                {
                  key: 'vat',
                  label: t('document.lines.vat'),
                  value: percentAndAmount(line.vatPercentage, line.vatAmount),
                },
                {
                  key: 'ait',
                  label: t('sales.lines.ait'),
                  value: percentAndAmount(line.aitPercentage, line.aitAmount),
                },
                {
                  key: 'warranty',
                  label: t('sales.lines.warranty'),
                  value: formatNumber(line.warrantyInMonth),
                },
                {
                  key: 'delivery',
                  label: t('quotation.lines.delivery'),
                  value: line.deliveryDate,
                },
                {
                  key: 'approved',
                  label: t('quotation.lines.approved'),
                  value:
                    line.approveQuantity > 0
                      ? `${formatNumber(line.approveQuantity, QUANTITY_DECIMALS_SHOWN)} × ${formatMoney(line.approvePrice)} = ${formatMoney(line.approveAmount)}`
                      : null,
                },
                {
                  key: 'description',
                  label: t('sales.lines.itemDescription'),
                  value: line.itemDescription,
                  wide: true,
                },
              ]}
            />
          )}
          totals={[
            {
              key: 'quantity',
              label: t('document.lines.totalQuantity'),
              value: formatNumber(quotation.totalQuantity, QUANTITY_DECIMALS_SHOWN),
            },
            {
              key: 'gross',
              label: t('sales.totals.gross'),
              value: formatMoney(quotation.grossAmount),
            },
            {
              key: 'discount',
              label: t('document.totals.discount'),
              value: formatMoney(quotation.discountAmount),
            },
            {
              key: 'sd',
              label: t('sales.totals.sd'),
              value: formatMoney(quotation.sdAmount),
            },
            {
              key: 'vat',
              label: t('document.totals.vat'),
              value: formatMoney(quotation.vatAmount),
            },
            {
              key: 'ait',
              label: t('sales.totals.ait'),
              value: formatMoney(quotation.aitAmount),
            },
            {
              key: 'charges',
              label: t('document.totals.charges'),
              value: formatMoney(quotation.deliveryCharge + quotation.extraCharge),
            },
            {
              key: 'total',
              label: t('sales.totals.total'),
              value: formatMoney(quotation.totalAmount),
              strong: true,
            },
          ]}
        />
      }
    />
  );
}
