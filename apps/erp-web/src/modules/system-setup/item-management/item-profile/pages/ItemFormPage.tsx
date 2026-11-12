import { CheckOutlined } from '@ant-design/icons';
import {
  getGetItemQueryKey,
  useCreateItem,
  useGetItem,
  useUpdateItem,
  type FeatureConfig,
  type ItemDetail,
} from '@erp/api-client';
import { defaultValues, useFormSections, useText } from '@erp/metadata-engine';
import { ForbiddenResult, FormPageLayout, PageState, useGoBack } from '@erp/ui';
import { App, Form } from 'antd';
import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate, useParams, useSearchParams } from 'react-router';
import { useCustomLookups } from '@/common/hooks/useCustomLookups';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { useFeatureConfig } from '@/common/hooks/useFeatureConfig';
import { applyFieldErrors, problemMessage } from '@/common/utils/api-errors';
import { useUnitKey } from '@/common/master-data';
import { useRefreshItemManagement } from '../../item-management';
import { ConversionInput } from '../components/ConversionInput';
import { ImageAttach } from '../components/ImageAttach';
import { MasterItemSelect } from '../components/MasterItemSelect';
import { ITEM_ASIDE_SECTIONS, ITEM_LOOKUPS, ITEM_PROFILE_FEATURE, itemPaths } from '../constants';
import { copyOf, itemBehaviors, toRequest, type ItemFormValues } from '../item-form';
import { useItemLookups } from '../item-lookups';

export function ItemFormPage() {
  const { t } = useTranslation();
  const { id } = useParams();
  const [params] = useSearchParams();
  const copyId = params.get('copy');
  const sourceId = Number(id ?? copyId ?? 0);
  const access = useFeatureAccess(ITEM_PROFILE_FEATURE);
  const allowed = id ? access.edit : access.create;
  const config = useFeatureConfig(ITEM_PROFILE_FEATURE, allowed);
  const source = useGetItem(sourceId, {
    query: { queryKey: useUnitKey(getGetItemQueryKey(sourceId)), enabled: allowed && sourceId > 0 },
  });

  if (!allowed) return <ForbiddenResult />;

  const loading = (sourceId > 0 && source.isPending) || config.isPending;

  return (
    <PageState
      loading={loading}
      missing={!loading && ((sourceId > 0 && !source.data) || !config.data)}
      missingTitle={t('item.notFound')}
      missingActions={[{ key: 'back', label: t('item.backToList'), to: itemPaths.list }]}
    >
      {config.data && (
        <ItemForm
          key={id ?? copyId ?? 'new'}
          config={config.data}
          item={id ? source.data : undefined}
          copyFrom={copyId ? source.data : undefined}
        />
      )}
    </PageState>
  );
}

function ItemForm({
  config,
  item,
  copyFrom,
}: {
  config: FeatureConfig;
  item?: ItemDetail;
  copyFrom?: ItemDetail;
}) {
  const { t } = useTranslation();
  const text = useText();
  const { message } = App.useApp();
  const navigate = useNavigate();
  const [form] = Form.useForm<ItemFormValues>();
  const lookups = { ...useCustomLookups(config), ...useItemLookups() };
  const create = useCreateItem();
  const update = useUpdateItem();
  const refresh = useRefreshItemManagement();
  const source = item ?? copyFrom;
  const goBack = useGoBack(item ? itemPaths.view(item.id) : itemPaths.list);

  const initialValues = useMemo(
    () =>
      item ??
      (copyFrom
        ? copyOf(copyFrom, t('item.copyName', { name: copyFrom.name }))
        : (defaultValues(config) as Partial<ItemFormValues>)),
    [item, copyFrom, config, t],
  );

  const { sections, aside } = useFormSections(config, {
    lookups,
    aside: ITEM_ASIDE_SECTIONS,
    behaviors: itemBehaviors(t),
    widgets: {
      imageSource: () => <ImageAttach />,
      masterItemId: (field) => (
        <MasterItemSelect
          source={source}
          excludeId={item?.id}
          placeholder={text(field.placeholder)}
        />
      ),
      altUomConversionQty: () => <ConversionInput uoms={lookups[ITEM_LOOKUPS.uom]} />,
    },
  });

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
        { key: 'new', label: t('item.form.saveNew') },
        { key: 'save', label: t('item.form.save'), icon: <CheckOutlined />, variant: 'primary' },
      ]}
      sections={sections}
      aside={aside}
    />
  );
}
