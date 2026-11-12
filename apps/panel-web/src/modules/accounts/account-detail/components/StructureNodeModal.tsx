import {
  ApiError,
  useAddStructureNode,
  useUpdateStructureNode,
  type StructureNodeRequest,
} from '@erp/api-client';
import { Field, FieldGrid, FormModal, YesNoSwitch } from '@erp/ui';
import { App, Form, Input, Select } from 'antd';
import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { normalizeCode } from '../../account-setup/model/setup-model';
import { setupRules } from '../../account-setup/model/rules';
import type { StructureTarget } from '../../components/StructureView';
import type { StructureUnit } from '../../components/structure';

const CODE_MAX = 32;
const NAME_MAX = 200;
const ADDRESS_MAX = 500;

type Values = Omit<StructureNodeRequest, 'kind' | 'businessUnitId'>;

export function StructureNodeModal({
  accountId,
  target,
  units,
  onClose,
  onSaved,
}: {
  accountId: string;
  target?: StructureTarget;
  units: StructureUnit[];
  onClose: () => void;
  onSaved: () => Promise<unknown>;
}) {
  const { t } = useTranslation();
  const { message } = App.useApp();
  const rules = setupRules(t);
  const [form] = Form.useForm<Values>();
  const add = useAddStructureNode();
  const update = useUpdateStructureNode();
  const node = target?.node;
  const unit = units.find((u) => u.key === target?.unitKey);
  const isWarehouse = target?.kind === 'warehouse';

  const initialValues = useMemo<Partial<Values>>(
    () => ({
      code: node?.code,
      name: node?.name,
      email: node?.email,
      phone: node?.phone,
      address: node?.address,
      officeId: target?.officeKey ?? null,
      isActive: node?.isActive ?? true,
    }),
    [node, target?.officeKey],
  );

  if (!target) return null;

  const submit = async (values: Values) => {
    const data: StructureNodeRequest = {
      ...values,
      kind: target.kind,
      businessUnitId: target.unitKey ?? null,
      officeId: isWarehouse ? (values.officeId ?? null) : null,
    };
    try {
      if (node) await update.mutateAsync({ id: accountId, nodeId: node.key, data });
      else await add.mutateAsync({ id: accountId, data });
      await onSaved();
      void message.success(t('structure.saved'));
    } catch (error) {
      if (error instanceof ApiError && error.status === 400)
        form.setFields(
          Object.entries(error.fieldErrors).map(([name, errors]) => ({
            name: name as keyof Values,
            errors,
          })),
        );
      void message.error(
        error instanceof ApiError
          ? (Object.values(error.fieldErrors)[0]?.[0] ?? error.message)
          : t('structure.failed'),
      );
      throw error;
    }
  };

  const kind = t(`structure.kind.${target.kind}`);

  return (
    <FormModal<Values>
      open={!!target}
      onClose={onClose}
      form={form}
      title={node ? t('structure.editTitle', { kind }) : t('structure.newTitle', { kind })}
      subtitle={unit && target.kind !== 'business-unit' ? unit.name : undefined}
      initialValues={initialValues}
      onSubmit={submit}
    >
      <FieldGrid columns={2}>
        <Field
          name="code"
          label={t('structure.f.code')}
          normalize={normalizeCode}
          rules={rules.code}
        >
          <Input className="erp-code" maxLength={CODE_MAX} />
        </Field>
        <Field
          name="name"
          label={t('structure.f.name')}
          rules={[rules.required, { max: NAME_MAX }]}
        >
          <Input maxLength={NAME_MAX} />
        </Field>
        {isWarehouse ? (
          <Field name="officeId" label={t('structure.office')} extra={t('structure.officeHint')}>
            <Select
              allowClear
              options={(unit?.offices ?? []).map((o) => ({
                value: o.key,
                label: `${o.name} (${o.code})`,
              }))}
            />
          </Field>
        ) : (
          <>
            <Field name="email" label={t('field.email')} rules={[rules.email]}>
              <Input />
            </Field>
            <Field name="phone" label={t('field.phone')} rules={[rules.phone]}>
              <Input />
            </Field>
          </>
        )}
        <Field name="isActive" label={t('accounts.status.active')} valuePropName="checked">
          <YesNoSwitch />
        </Field>
      </FieldGrid>
      <Field name="address" label={t('field.address')} rules={[{ max: ADDRESS_MAX }]}>
        <Input.TextArea autoSize={{ minRows: 2, maxRows: 4 }} maxLength={ADDRESS_MAX} />
      </Field>
    </FormModal>
  );
}
