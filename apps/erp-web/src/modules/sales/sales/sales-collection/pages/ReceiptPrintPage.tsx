import {
  getGetSalesCollectionQueryKey,
  useGetSalesCollection,
  type SalesCollectionLineDetail,
} from '@erp/api-client';
import { ForbiddenResult, formatDate, formatMoney } from '@erp/ui';
import { useTranslation } from 'react-i18next';
import { useParams } from 'react-router';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { useUnitKey } from '@/common/master-data';
import { SalesPrintPage } from '../../../print/SalesPrintPage';
import { AMOUNTS, COLLECTION_FEATURE, collectionPaths } from '../constants';

const joined = (...values: (string | null | undefined)[]) => values.filter(Boolean).join(' · ');

export function ReceiptPrintPage() {
  const { t } = useTranslation();
  const id = Number(useParams().id);
  const access = useFeatureAccess(COLLECTION_FEATURE);
  const query = useGetSalesCollection(id, {
    query: { queryKey: useUnitKey(getGetSalesCollectionQueryKey(id)), enabled: access.view },
  });

  if (!access.view) return <ForbiddenResult />;

  const collection = query.data;
  const amount = (value: number) => formatMoney(value, { symbol: false });
  const used = AMOUNTS.filter((key) => collection?.[key]);
  const column = (
    key: 'billAmount' | 'settledAmount' | (typeof AMOUNTS)[number],
    label: string,
  ) => ({
    key,
    title: label,
    end: true,
    render: (line: SalesCollectionLineDetail) => amount(line[key]),
  });

  return (
    <SalesPrintPage<SalesCollectionLineDetail>
      title={t('print.title.receipt')}
      backTo={collectionPaths.view(id)}
      loading={query.isPending}
      record={collection}
      notFound={t('collection.notFound')}
      partyTitle={t('print.receivedFrom')}
      party={(customer) => [
        { label: t('print.customerCode'), value: customer.code },
        { label: t('print.phone'), value: customer.phone },
        { label: t('print.address'), value: customer.address },
      ]}
      facts={[
        { label: t('print.f.receiptNo'), value: collection?.moneyReceiptNo ?? collection?.code },
        {
          label: t('print.f.collectionNo'),
          value: collection?.moneyReceiptNo && collection.code,
        },
        { label: t('print.f.date'), value: formatDate(collection?.collectionDate) },
        { label: t('print.f.method'), value: collection?.paymentMethodName },
        { label: t('print.f.account'), value: collection?.bankAccountName },
        {
          label: t('print.f.instrument'),
          value: joined(
            collection?.instrumentTypeName,
            collection?.instrumentNo,
            collection?.instrumentDate && formatDate(collection.instrumentDate),
          ),
        },
        {
          label: t('print.f.customerBank'),
          value: joined(collection?.customerBankName, collection?.customerBankBranch),
        },
        { label: t('print.f.transactionNo'), value: collection?.transactionNo },
        { label: t('print.f.reference'), value: collection?.referenceNo },
        { label: t('print.f.collectedBy'), value: collection?.collectedByName },
      ]}
      intro={
        <>
          {t('print.received')} <strong>{formatMoney(collection?.collectionAmount)}</strong>
        </>
      }
      columns={[
        {
          key: 'bill',
          title: t('collection.lines.bill'),
          render: (line) => joined(line.deliveryCode, formatDate(line.deliveryDate)),
        },
        column('billAmount', t('collection.lines.billAmount')),
        ...used.map((key) => column(key, t(`collection.lines.${key}`))),
        column('settledAmount', t('collection.lines.settled')),
      ]}
      lines={collection?.lines ?? []}
      totals={
        collection
          ? [
              ...(collection.isAdvance
                ? []
                : [
                    ...used
                      .filter((key) => key !== 'collectionAmount')
                      .map((key) => ({
                        label: t(`collection.lines.${key}`),
                        value: amount(collection[key]),
                      })),
                    {
                      label: t('collection.lines.settled'),
                      value: amount(collection.settledAmount),
                    },
                  ]),
              {
                label: t('print.totalReceived'),
                value: formatMoney(collection.collectionAmount),
                strong: true,
              },
            ]
          : []
      }
      amount={collection?.collectionAmount}
      notes={[collection?.isAdvance && t('print.advanceReceipt'), collection?.remarks]}
      signatures={[
        t('print.sign.customer'),
        t('print.sign.receivedBy'),
        t('print.sign.authorised'),
      ]}
    />
  );
}
