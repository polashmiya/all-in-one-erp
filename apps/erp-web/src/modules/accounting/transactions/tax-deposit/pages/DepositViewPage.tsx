import { BankOutlined, CalendarOutlined, FileDoneOutlined } from '@ant-design/icons';
import {
  getGetTaxDepositQueryKey,
  useGetTaxDeposit,
  type TaxDepositLineDetail,
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
import '@/common/documents/documents.css';
import { DocumentView } from '@/common/documents/DocumentView';
import { LineTotals } from '@/common/documents/LineTotals';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { useFeatureConfig } from '@/common/hooks/useFeatureConfig';
import { useUnitKey } from '@/common/master-data';
import { paymentPaths } from '@/modules/procurement/purchase/purchase-payment/constants';
import { DEPOSIT_FEATURE, depositPaths } from '../constants';
import { depositCalls } from '../deposit-form';

export function DepositViewPage() {
  const { t } = useTranslation();
  const id = Number(useParams().id);
  const access = useFeatureAccess(DEPOSIT_FEATURE);
  const config = useFeatureConfig(DEPOSIT_FEATURE, access.view);
  const query = useGetTaxDeposit(id, {
    query: { queryKey: useUnitKey(getGetTaxDepositQueryKey(id)), enabled: access.view },
  });

  if (!access.view) return <ForbiddenResult />;

  const deposit = query.data;
  if (!deposit || !config.data) {
    const loading = query.isPending || config.isPending;
    return (
      <DetailsPageLayout
        loading={loading}
        missing={!loading}
        missingTitle={t('deposit.notFound')}
        missingActions={[{ key: 'back', label: t('deposit.backToList'), to: depositPaths.list }]}
      />
    );
  }

  const typeName = t(`deposit.type.${deposit.type}`);

  return (
    <DocumentView
      document={deposit}
      config={config.data}
      feature={DEPOSIT_FEATURE}
      paths={depositPaths}
      calls={depositCalls}
      icon={<BankOutlined />}
      tag={typeName}
      renderers={{ type: () => typeName }}
      meta={[
        { key: 'date', icon: <CalendarOutlined />, label: formatDate(deposit.depositDate) },
        { key: 'challan', icon: <FileDoneOutlined />, label: deposit.challanNo },
        { key: 'bank', icon: <BankOutlined />, label: deposit.bankAccountName },
      ]}
      lines={
        <DetailSection
          title={t('deposit.lines.title')}
          description={t('deposit.paymentCount', { count: deposit.lines.length })}
        >
          <div className="erp-lines">
            <Table<TaxDepositLineDetail>
              size="small"
              rowKey="id"
              pagination={false}
              dataSource={deposit.lines}
              scroll={{ x: 'max-content' }}
              columns={[
                {
                  key: 'payment',
                  title: t('deposit.lines.payment'),
                  render: (_, line) => (
                    <TitleCell
                      title={
                        <Link to={paymentPaths.view(line.purchasePaymentId)}>
                          {line.paymentCode}
                        </Link>
                      }
                      subtitle={formatDate(line.paymentDate)}
                    />
                  ),
                },
                {
                  key: 'supplier',
                  title: t('deposit.lines.supplier'),
                  render: (_, line) => line.supplierName ?? '—',
                },
                {
                  key: 'deducted',
                  title: t('deposit.lines.deducted'),
                  align: 'end',
                  render: (_, line) => formatMoney(line.deducted),
                },
                {
                  key: 'amount',
                  title: t('deposit.lines.amount'),
                  align: 'end',
                  render: (_, line) => formatMoney(line.amount),
                },
              ]}
            />
            <div className="erp-lines__footer">
              <span />
              <LineTotals
                items={[
                  {
                    key: 'total',
                    label: t('deposit.total'),
                    value: formatMoney(deposit.totalAmount),
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
