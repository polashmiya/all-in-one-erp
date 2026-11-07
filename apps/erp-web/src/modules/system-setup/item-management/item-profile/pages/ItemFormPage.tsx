import { CheckOutlined } from '@ant-design/icons';
import {
  getGetItemQueryKey,
  useCreateItem,
  useGetItem,
  useUpdateItem,
  type ItemDetail,
} from '@erp/api-client';
import { ForbiddenResult, FormPageLayout, PageState, useGoBack } from '@erp/ui';
import { App, Form } from 'antd';
import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate, useParams, useSearchParams } from 'react-router';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { applyFieldErrors, problemMessage } from '@/common/utils/api-errors';
import { useRefreshItemManagement, useUnitKey } from '../../common/master-data';
import { GeneralSection } from '../components/GeneralSection';
import { PricingSection } from '../components/PricingSection';
import { StatusSection } from '../components/StatusSection';
import { TaxSection } from '../components/TaxSection';
import { ITEM_PROFILE_FEATURE, itemPaths } from '../constants';
import { copyOf, ITEM_TAB_FIELDS, NEW_ITEM, toRequest, type ItemFormValues } from '../item-form';
import { useItemLookups } from '../item-lookups';

export function ItemFormPage() {
  const { t } = useTranslation();
  const { id } = useParams();
  const [params] = useSearchParams();
  const copyId = params.get('copy');
  const sourceId = Number(id ?? copyId ?? 0);
  const access = useFeatureAccess(ITEM_PROFILE_FEATURE);
  const allowed = id ? access.edit : access.create;
  const source = useGetItem(sourceId, {
    query: { queryKey: useUnitKey(getGetItemQueryKey(sourceId)), enabled: allowed && sourceId > 0 },
  });

  if (!allowed) return <ForbiddenResult />;

  const loading = sourceId > 0 && source.isPending;

  return (
    <PageState
      loading={loading}
      missing={sourceId > 0 && !loading && !source.data}
      missingTitle={t('item.notFound')}
      missingActions={[{ key: 'back', label: t('item.backToList'), to: itemPaths.list }]}
    >
      <ItemForm
        key={id ?? copyId ?? 'new'}
        item={id ? source.data : undefined}
        copyFrom={copyId ? source.data : undefined}
      />
    </PageState>
  );
}

function ItemForm({ item, copyFrom }: { item?: ItemDetail; copyFrom?: ItemDetail }) {
  const { t } = useTranslation();
  const { message } = App.useApp();
  const navigate = useNavigate();
  const [form] = Form.useForm<ItemFormValues>();
  const lookups = useItemLookups();
  const create = useCreateItem();
  const update = useUpdateItem();
  const refresh = useRefreshItemManagement();
  const source = item ?? copyFrom;
  const goBack = useGoBack(item ? itemPaths.view(item.id) : itemPaths.list);

  const initialValues = useMemo(
    () =>
      item ?? (copyFrom ? copyOf(copyFrom, t('item.copyName', { name: copyFrom.name })) : NEW_ITEM),
    [item, copyFrom, t],
  );

  const submit = async (values: ItemFormValues, action: string) => {
    try {
      const data = toRequest(values);
      const saved = item
        ? await update.mutateAsync({ id: item.id, data })
        : await create.mutateAsync({ data });
      await refresh();
      void message.success(t('item.form.saved', { code: saved.code }));
      if (action === 'new') {
        if (source) void navigate(itemPaths.create);
        else form.resetFields();
      } else if (item) goBack();
      else void navigate(itemPaths.view(saved.id), { replace: true });
    } catch (error) {
      void message.error(
        applyFieldErrors(form, error)
          ? t('master.fixFields')
          : problemMessage(error, t('master.failed')),
      );
      throw error;
    }
  };

  return (
    <FormPageLayout<ItemFormValues>
      form={form}
      initialValues={initialValues}
      title={item ? t('item.form.editTitle') : t('item.form.newTitle')}
      status={
        item
          ? item.isActive
            ? { tone: 'success', label: t('master.active') }
            : { tone: 'neutral', label: t('master.inactive') }
          : undefined
      }
      backTo={item ? itemPaths.view(item.id) : itemPaths.list}
      crumbs={[item ? item.code : t('item.form.newTitle')]}
      cancelTo={item ? itemPaths.view(item.id) : itemPaths.list}
      saving={create.isPending || update.isPending}
      onSubmit={submit}
      submitActions={[
        { key: 'new', label: t('item.form.saveNew'), placement: ['footer'] },
        { key: 'save', label: t('item.form.save'), icon: <CheckOutlined />, variant: 'primary' },
      ]}
      tabs={[
        {
          key: 'general',
          label: t('item.tab.general'),
          fields: ITEM_TAB_FIELDS.general,
          content: <GeneralSection source={source} itemId={item?.id} lookups={lookups} />,
        },
        {
          key: 'pricing',
          label: t('item.tab.pricing'),
          fields: ITEM_TAB_FIELDS.pricing,
          content: <PricingSection source={source} lookups={lookups} />,
        },
        {
          key: 'tax',
          label: t('item.tab.tax'),
          fields: ITEM_TAB_FIELDS.tax,
          content: <TaxSection lookups={lookups} />,
        },
        {
          key: 'status',
          label: t('item.tab.status'),
          fields: ITEM_TAB_FIELDS.status,
          content: <StatusSection />,
        },
      ]}
    />
  );
}
