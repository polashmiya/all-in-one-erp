import { BankOutlined, CalendarOutlined, ShopOutlined, WalletOutlined } from '@ant-design/icons';
import {
  getGetPurchasePaymentQueryKey,
  useGetPurchasePayment,
  type PurchasePaymentLineDetail,
} from '@erp/api-client';
import {
  DetailSection,
  DetailsPageLayout,
  ForbiddenResult,
  formatDate,
  formatMoney,
  TitleCell,
} from '@erp/ui';
import { Table } from 'antd';
import { useTranslation } from 'react-i18next';
import { Link, useParams } from 'react-router';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { useFeatureConfig } from '@/common/hooks/useFeatureConfig';
import { useUnitKey } from '@/common/master-data';
import { DocumentView } from '../../../components/DocumentView';
import { LineTotals } from '../../../components/LineTotals';
import { receivePaths } from '../../purchase-receive/constants';
import { PAYMENT_FEATURE, paymentPaths } from '../constants';
import { paymentCalls } from '../payment-form';
import '../../../procurement.css';

type MoneyKey =
  'billAmount' | 'due' | 'paymentAmount' | 'vds' | 'tds' | 'discountAmount' | 'settledAmount';

export function PaymentViewPage() {
  const { t } = useTranslation();
  const id = Number(useParams().id);
  const access = useFeatureAccess(PAYMENT_FEATURE);
  const config = useFeatureConfig(PAYMENT_FEATURE, access.view);
  const query = useGetPurchasePayment(id, {
    query: { queryKey: useUnitKey(getGetPurchasePaymentQueryKey(id)), enabled: access.view },
  });

  if (!access.view) return <ForbiddenResult />;

  const payment = query.data;
  if (!payment || !config.data) {
    const loading = query.isPending || config.isPending;
    return (
      <DetailsPageLayout
        loading={loading}
        missing={!loading}
        missingTitle={t('payment.notFound')}
        missingActions={[{ key: 'back', label: t('payment.backToList'), to: paymentPaths.list }]}
      />
    );
  }

  const money = (key: MoneyKey, title: string) => ({
    key,
    title,
    align: 'end' as const,
    render: (_: unknown, line: PurchasePaymentLineDetail) => formatMoney(line[key]),
  });

  return (
    <DocumentView
      document={payment}
      config={config.data}
      feature={PAYMENT_FEATURE}
      paths={paymentPaths}
      calls={paymentCalls}
      icon={<WalletOutlined />}
      tag={payment.paymentMethodName}
      meta={[
        { key: 'supplier', icon: <ShopOutlined />, label: payment.supplierName },
        {
          key: 'date',
          icon: <CalendarOutlined />,
          label: t('payment.meta.paid', { date: formatDate(payment.paymentDate) }),
        },
        {
          key: 'instrument',
          icon: <BankOutlined />,
          label: [
            payment.instrumentNo,
            payment.instrumentDate && formatDate(payment.instrumentDate),
          ]
            .filter(Boolean)
            .join(' · '),
        },
      ]}
      lines={
        <DetailSection
          title={t('payment.lines.title')}
          description={t('payment.billCount', { count: payment.lines.length })}
        >
          <div className="erp-lines">
            <Table<PurchasePaymentLineDetail>
              size="small"
              rowKey="id"
              pagination={false}
              dataSource={payment.lines}
              scroll={{ x: 'max-content' }}
              columns={[
                {
                  key: 'bill',
                  title: t('payment.lines.bill'),
                  render: (_, line) => (
                    <TitleCell
                      title={
                        <Link to={receivePaths.view(line.purchaseReceiveId)}>
                          {line.purchaseReceiveCode}
                        </Link>
                      }
                      subtitle={[formatDate(line.receiveDate), line.purchaseOrderName]
                        .filter(Boolean)
                        .join(' · ')}
                    />
                  ),
                },
                money('billAmount', t('payment.lines.billAmount')),
                money('due', t('payment.lines.due')),
                money('paymentAmount', t('payment.lines.paymentAmount')),
                money('vds', t('payment.lines.vds')),
                money('tds', t('payment.lines.tds')),
                money('discountAmount', t('payment.lines.discountAmount')),
                money('settledAmount', t('payment.lines.settled')),
              ]}
            />
            <div className="erp-lines__footer">
              <span />
              <LineTotals
                items={[
                  {
                    key: 'payment',
                    label: t('payment.totals.payment'),
                    value: formatMoney(payment.paymentAmount),
                  },
                  { key: 'vds', label: t('payment.totals.vds'), value: formatMoney(payment.vds) },
                  { key: 'tds', label: t('payment.totals.tds'), value: formatMoney(payment.tds) },
                  {
                    key: 'discount',
                    label: t('payment.totals.discount'),
                    value: formatMoney(payment.discountAmount),
                  },
                  {
                    key: 'settled',
                    label: t('payment.totals.settled'),
                    value: formatMoney(payment.settledAmount),
                    strong: true,
                  },
                ]}
              />
            </div>
          </div>
        </DetailSection>
      }
    />
  );
}
