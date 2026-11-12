import {
  useCreateUom,
  useUpdateUom,
  type FeatureConfig,
  type SaveUomRequest,
  type UomRow,
} from '@erp/api-client';
import type { FieldBehavior } from '@erp/metadata-engine';
import { Form } from 'antd';
import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { MasterFormModal, trimmed } from '@/common/master-data';

const uomBehaviors: Record<string, FieldBehavior> = {
  roundingLength: {
    watch: ['isDecimalAllowed'],
    state: (v) => ({ hidden: !v.isDecimalAllowed, required: true }),
  },
};

export function UomFormModal({
  open,
  uom,
  config,
  accountName,
  onClose,
  onSaved,
}: {
  open: boolean;
  uom?: UomRow;
  config: FeatureConfig;
  accountName: string;
  onClose: () => void;
  onSaved: () => Promise<unknown>;
}) {
  const { t } = useTranslation();
  const [form] = Form.useForm<SaveUomRequest>();
  const create = useCreateUom();
  const update = useUpdateUom();
  const initialValues = useMemo<Partial<SaveUomRequest>>(() => {
    if (!uom) return {};
    const { roundingLength, ...rest } = uom;
    return uom.isDecimalAllowed ? { ...rest, roundingLength } : rest;
  }, [uom]);

  const save = async (values: SaveUomRequest) => {
    const data: SaveUomRequest = {
      code: values.code.trim(),
      name: values.name.trim(),
      description: trimmed(values.description),
      isDecimalAllowed: !!values.isDecimalAllowed,
      roundingLength: values.isDecimalAllowed ? (values.roundingLength ?? null) : null,
      isActive: values.isActive ?? true,
      customData: values.customData ?? null,
    };
    const saved = uom
      ? await update.mutateAsync({ id: uom.id, data })
      : await create.mutateAsync({ data });
    await onSaved();
    return saved;
  };

  return (
    <MasterFormModal<SaveUomRequest>
      open={open}
      onClose={onClose}
      title={uom ? t('uom.form.editTitle') : t('uom.form.newTitle')}
      subtitle={t('uom.form.subtitle', { account: accountName })}
      form={form}
      config={config}
      options={{ behaviors: uomBehaviors }}
      initialValues={initialValues}
      editing={!!uom}
      onSave={save}
    />
  );
}
