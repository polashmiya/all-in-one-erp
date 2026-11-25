import {
  getGetCollectableBillsQueryKey,
  getGetSalesCollectionQueryKey,
  useCreateSalesCollection,
  useGetCollectableBills,
  useGetSalesCollection,
  useUpdateSalesCollection,
} from '@erp/api-client';
import { defaultValues } from '@erp/metadata-engine';
import { ForbiddenResult, PageState } from '@erp/ui';
import { useTranslation } from 'react-i18next';
import { useParams, useSearchParams } from 'react-router';
import { DocumentForm } from '@/common/documents/DocumentForm';
import { useFormDraft } from '@/common/hooks/useDrafts';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { useFeatureConfig } from '@/common/hooks/useFeatureConfig';
import { useUnitKey } from '@/common/master-data';
import { FORM_COLUMNS } from '../../../sales-document';
import {
  collectionBehaviors,
  fromBill,
  fromDetail,
  newCollection,
  toRequest,
  useCollectionLookups,
  type CollectionValues,
} from '../collection-form';
import { CollectionLines } from '../components/CollectionLines';
import { CustomerBillsPicker } from '../components/CustomerBillsPicker';
import {
  COLLECTION_FEATURE,
  COLLECTION_LOOKUPS,
  collectionPaths,
  CUSTOMER_PARAM,
} from '../constants';

export function CollectionFormPage() {
  const { t } = useTranslation();
  const id = Number(useParams().id ?? 0);
  const [search] = useSearchParams();
  const customerId = id ? 0 : Number(search.get(CUSTOMER_PARAM) ?? 0);
  const access = useFeatureAccess(COLLECTION_FEATURE);
  const allowed = id ? access.edit : access.create;
  const config = useFeatureConfig(COLLECTION_FEATURE, allowed);
  const draft = useFormDraft<CollectionValues>(COLLECTION_FEATURE, allowed && !id);
  const source = useGetSalesCollection(id, {
    query: { queryKey: useUnitKey(getGetSalesCollectionQueryKey(id)), enabled: allowed && id > 0 },
  });
  const params = { customerId };
  const bills = useGetCollectableBills(params, {
    query: {
      queryKey: useUnitKey(getGetCollectableBillsQueryKey(params)),
      enabled: allowed && customerId > 0,
    },
  });
  const { lookups, payFrom } = useCollectionLookups(allowed);
  const create = useCreateSalesCollection();
  const update = useUpdateSalesCollection();

  if (!allowed) return <ForbiddenResult />;

  const loading =
    (id > 0 && source.isPending) ||
    config.isPending ||
    draft.loading ||
    (customerId > 0 && bills.isPending);
  const record = id ? source.data : undefined;
  const locked = !!record && record.status !== 'pending';

  return (
    <PageState
      loading={loading}
      missing={!loading && ((id > 0 && !record) || !config.data || locked)}
      missingTitle={locked ? t('document.locked') : t('collection.notFound')}
      missingActions={[
        record
          ? { key: 'back', label: record.code, to: collectionPaths.view(record.id) }
          : { key: 'back', label: t('collection.backToList'), to: collectionPaths.list },
      ]}
    >
      {config.data && (
        <DocumentForm<CollectionValues>
          key={id || `new-${customerId}`}
          config={config.data}
          record={record}
          draft={draft}
          initialValues={
            record
              ? fromDetail(record)
              : {
                  ...defaultValues(config.data),
                  ...newCollection(),
                  ...(bills.data && { customerId, lines: bills.data.bills.map(fromBill) }),
                  ...draft.values,
                }
          }
          options={{
            lookups,
            behaviors: collectionBehaviors(payFrom),
            maxColumns: FORM_COLUMNS,
            widgets: {
              customerId: (_, placeholder) => (
                <CustomerBillsPicker
                  customers={lookups[COLLECTION_LOOKUPS.customer]}
                  collectionId={record?.id}
                  placeholder={placeholder}
                />
              ),
            },
          }}
          lines={<CollectionLines collectionId={record?.id} payFrom={payFrom} />}
          paths={collectionPaths}
          texts={{
            newTitle: t('collection.form.newTitle'),
            editTitle: t('collection.form.editTitle'),
            save: t('collection.form.save'),
            saved: (code) => t('collection.form.saved', { code }),
          }}
          saving={create.isPending || update.isPending}
          onSave={(values, recordId) => {
            const data = toRequest(values, payFrom.isCash(values.paymentMethodId));
            return recordId
              ? update.mutateAsync({ id: recordId, data })
              : create.mutateAsync({ data });
          }}
        />
      )}
    </PageState>
  );
}
