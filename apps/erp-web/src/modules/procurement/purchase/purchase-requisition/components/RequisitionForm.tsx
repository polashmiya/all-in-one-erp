import { PlusOutlined, SaveOutlined } from '@ant-design/icons';
import {
  applyFieldErrors,
  problemMessage,
  useCreatePurchaseRequisition,
  useUpdatePurchaseRequisition,
  type DraftState,
  type FeatureConfig,
  type RequisitionDetail,
} from '@erp/api-client';
import { defaultValues, useFormSections } from '@erp/metadata-engine';
import { FormPageLayout, useGoBack } from '@erp/ui';
import { App, Form } from 'antd';
import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router';
import { useCustomLookups } from '@/common/hooks/useCustomLookups';
import { useRefreshProcurement } from '../../../procurement';
import { requisitionPaths, STATUS_TONES } from '../constants';
import {
  fromDetail,
  newRequisition,
  toRequest,
  useRequisitionLookups,
  type RequisitionValues,
} from '../requisition-form';
import { RequisitionLines } from './RequisitionLines';

export function RequisitionForm({
  config,
  record,
  draft,
}: {
  config: FeatureConfig;
  record?: RequisitionDetail;
  draft: DraftState<RequisitionValues>;
}) {
  const { t } = useTranslation();
  const { message } = App.useApp();
  const navigate = useNavigate();
  const [form] = Form.useForm<RequisitionValues>();
  const refresh = useRefreshProcurement();
  const create = useCreatePurchaseRequisition();
  const update = useUpdatePurchaseRequisition();
  const lookups = { ...useCustomLookups(config), ...useRequisitionLookups() };
  const goBack = useGoBack(record ? requisitionPaths.view(record.id) : requisitionPaths.list);

  const initialValues = useMemo<Partial<RequisitionValues>>(
    () =>
      record
        ? fromDetail(record)
        : {
            ...(defaultValues(config) as Partial<RequisitionValues>),
            ...newRequisition(),
            ...draft.values,
          },
    [record, config, draft.values],
  );

  const { sections } = useFormSections(config, { lookups });

  const submit = async (values: RequisitionValues, action: string) => {
    try {
      const data = toRequest(values);
      const saved = record
        ? await update.mutateAsync({ id: record.id, data })
        : await create.mutateAsync({ data });
      const fromDraft = draft.draft.continuing;
      await draft.finish();
      await refresh();
      void message.success(t('requisition.form.saved', { code: saved.code }));
      if (action === 'new') {
        if (fromDraft) void navigate(requisitionPaths.create);
        else form.resetFields();
      } else if (record) goBack();
      else void navigate(requisitionPaths.view(saved.id), { replace: true });
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
    <FormPageLayout<RequisitionValues>
      form={form}
      initialValues={initialValues}
      title={record ? t('requisition.form.editTitle') : t('requisition.form.newTitle')}
      status={
        record
          ? {
              tone: STATUS_TONES[record.status] ?? 'neutral',
              label: t(`requisition.status.${record.status}`),
            }
          : undefined
      }
      backTo={record ? requisitionPaths.view(record.id) : requisitionPaths.list}
      crumbs={[record ? record.code : t('requisition.form.newTitle')]}
      cancelTo={record ? requisitionPaths.view(record.id) : requisitionPaths.list}
      saving={create.isPending || update.isPending}
      onSubmit={submit}
      draft={record ? undefined : draft.draft}
      submitActions={[
        {
          key: 'new',
          label: t('requisition.form.saveNew'),
          icon: <PlusOutlined />,
          description: t('common.saveNewHint'),
          hidden: !!record,
        },
        {
          key: 'save',
          label: t('requisition.form.save'),
          icon: <SaveOutlined />,
          variant: 'primary',
        },
      ]}
      sections={[...sections, { key: 'lines', fields: ['lines'], content: <RequisitionLines /> }]}
    />
  );
}
