import {
  BankOutlined,
  CalendarOutlined,
  FileTextOutlined,
  IdcardOutlined,
  UserOutlined,
  WalletOutlined,
} from '@ant-design/icons';
import {
  getGetSalesCollectionQueryKey,
  useGetSalesCollection,
  type SalesCollectionDetail,
  type SalesCollectionLineDetail,
} from '@erp/api-client';
import {
  DescriptionList,
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
import { DocumentView } from '@/common/documents/DocumentView';
import { LineTotals } from '@/common/documents/LineTotals';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { useFeatureConfig } from '@/common/hooks/useFeatureConfig';
import { useUnitKey } from '@/common/master-data';
import '@/common/documents/documents.css';
import { deliveryPaths } from '../../sales-delivery/constants';
import { collectionCalls } from '../collection-form';
import { AMOUNTS, COLLECTION_FEATURE, collectionPaths } from '../constants';

type MoneyKey = 'billAmount' | 'due' | 'collectionAmount' | 'settledAmount';

export function CollectionViewPage() {
  const { t } = useTranslation();
  const id = Number(useParams().id);
  const access = useFeatureAccess(COLLECTION_FEATURE);
  const config = useFeatureConfig(COLLECTION_FEATURE, access.view);
  const query = useGetSalesCollection(id, {
    query: { queryKey: useUnitKey(getGetSalesCollectionQueryKey(id)), enabled: access.view },
  });

  if (!access.view) return <ForbiddenResult />;

  const collection = query.data;
  if (!collection || !config.data) {
    const loading = query.isPending || config.isPending;
    return (
      <DetailsPageLayout
        loading={loading}
        missing={!loading}
        missingTitle={t('collection.notFound')}
        missingActions={[
          { key: 'back', label: t('collection.backToList'), to: collectionPaths.list },
        ]}
      />
    );
  }

  const money = (key: MoneyKey, title: string) => ({
    key,
    title,
    align: 'end' as const,
    render: (_: unknown, line: SalesCollectionLineDetail) => formatMoney(line[key]),
  });
  const deductions = AMOUNTS.filter((key) => key !== 'collectionAmount');

  return (
    <DocumentView<SalesCollectionDetail>
      document={collection}
      config={config.data}
      feature={COLLECTION_FEATURE}
      paths={collectionPaths}
      calls={collectionCalls}
      icon={<WalletOutlined />}
      tag={collection.isAdvance ? t('collection.advance') : collection.paymentMethodName}
      meta={[
        { key: 'customer', icon: <UserOutlined />, label: collection.customerName },
        {
          key: 'date',
          icon: <CalendarOutlined />,
          label: t('collection.meta.collected', { date: formatDate(collection.collectionDate) }),
        },
        {
          key: 'instrument',
          icon: <BankOutlined />,
          label: [collection.bankAccountName, collection.instrumentNo, collection.transactionNo]
            .filter(Boolean)
            .join(' · '),
        },
        { key: 'receipt', icon: <FileTextOutlined />, label: collection.moneyReceiptNo },
        { key: 'collectedBy', icon: <IdcardOutlined />, label: collection.collectedByName },
      ]}
      lines={
        collection.isAdvance ? (
          <DetailSection title={t('collection.lines.title')}>
            <span>{t('collection.lines.advanceDescription')}</span>
          </DetailSection>
        ) : (
          <DetailSection
            title={t('collection.lines.title')}
            description={t('collection.billCount', { count: collection.lines.length })}
          >
            <div className="erp-lines">
              <Table<SalesCollectionLineDetail>
                size="small"
                rowKey="id"
                pagination={false}
                dataSource={collection.lines}
                scroll={{ x: 'max-content' }}
                columns={[
                  {
                    key: 'bill',
                    title: t('collection.lines.bill'),
                    render: (_, line) => (
                      <TitleCell
                        title={
                          <Link to={deliveryPaths.view(line.deliveryId)}>{line.deliveryCode}</Link>
                        }
                        subtitle={[formatDate(line.deliveryDate), line.orderCode]
                          .filter(Boolean)
                          .join(' · ')}
                      />
                    ),
                  },
                  money('billAmount', t('collection.lines.billAmount')),
                  money('due', t('collection.lines.due')),
                  money('collectionAmount', t('collection.lines.collectionAmount')),
                  money('settledAmount', t('collection.lines.settled')),
                ]}
                expandable={{
                  expandedRowRender: (line) => (
                    <div className="erp-lines__details">
                      <DescriptionList
                        columns={4}
                        items={[
                          ...deductions.map((key) => ({
                            key,
                            label: t(`collection.lines.${key}`),
                            value: formatMoney(line[key]),
                          })),
                          {
                            key: 'remarks',
                            label: t('collection.lines.remarks'),
                            value: line.remarks,
                          },
                        ]}
                      />
                    </div>
                  ),
                  expandedRowKeys: collection.lines.map((line) => line.id),
                  showExpandColumn: false,
                }}
              />
              <div className="erp-lines__footer">
                <span />
                <LineTotals
                  items={[
                    ...AMOUNTS.map((key) => ({
                      key,
                      label: t(`collection.lines.${key}`),
                      value: formatMoney(collection[key]),
                    })),
                    {
                      key: 'settled',
                      label: t('collection.lines.settled'),
                      value: formatMoney(collection.settledAmount),
                      strong: true,
                    },
                  ]}
                />
              </div>
            </div>
          </DetailSection>
        )
      }
    />
  );
}
