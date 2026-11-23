import {
  ArrowDownOutlined,
  ArrowUpOutlined,
  DeleteOutlined,
  PlusOutlined,
} from '@ant-design/icons';
import type { ApproverOption, ApproverOptions } from '@erp/api-client';
import { DetailSection, EmptyState, Field } from '@erp/ui';
import { Button, Form, Input, Segmented, Select } from 'antd';
import { useTranslation } from 'react-i18next';
import '../account-approvals.css';

export type ApproverKind = 'role' | 'user';

export interface LevelValue {
  name?: string;
  kind: ApproverKind;
  approverId?: string;
}

export const NAME_MAX = 100;
export const MAX_LEVELS = 10;

export function LevelsEditor({ approvers }: { approvers?: ApproverOptions }) {
  const { t } = useTranslation();
  const form = Form.useFormInstance<{ levels: LevelValue[] }>();
  const levels = Form.useWatch('levels', form) ?? [];

  const options = (items: ApproverOption[] = []) =>
    items.map((item) => ({
      value: item.id,
      label: item.isActive ? item.name : t('approvals.inactive', { name: item.name }),
      title: item.email ?? item.name,
    }));

  return (
    <DetailSection title={t('approvals.levels')} description={t('approvals.levelsHint')}>
      <Form.List name="levels">
        {(fields, { add, remove, move }) => (
          <div className="acc-levels">
            {fields.length === 0 && <EmptyState title={t('approvals.none')} />}
            {fields.map((field, index) => {
              const kind = levels[field.name]?.kind ?? 'role';
              return (
                <div key={field.key} className="erp-card acc-levels__item">
                  <span className="acc-levels__badge">{index + 1}</span>
                  <Field
                    name={[field.name, 'name']}
                    label={t('approvals.level', { level: index + 1 })}
                    rules={[
                      { required: true, whitespace: true, message: t('approvals.nameRequired') },
                    ]}
                  >
                    <Input maxLength={NAME_MAX} placeholder={t('approvals.namePlaceholder')} />
                  </Field>
                  <Field name={[field.name, 'kind']} label={t('approvals.by')}>
                    <Segmented
                      options={[
                        { value: 'role', label: t('approvals.role') },
                        { value: 'user', label: t('approvals.user') },
                      ]}
                      onChange={() =>
                        form.setFieldValue(['levels', field.name, 'approverId'], undefined)
                      }
                    />
                  </Field>
                  <Field
                    name={[field.name, 'approverId']}
                    label={t('approvals.approver')}
                    rules={[{ required: true, message: t('approvals.approverRequired') }]}
                  >
                    <Select
                      showSearch
                      optionFilterProp="label"
                      placeholder={t(kind === 'role' ? 'approvals.role' : 'approvals.user')}
                      options={options(kind === 'role' ? approvers?.roles : approvers?.users)}
                    />
                  </Field>
                  <div className="acc-levels__actions">
                    <Button
                      type="text"
                      icon={<ArrowUpOutlined />}
                      aria-label={t('approvals.up')}
                      title={t('approvals.up')}
                      disabled={index === 0}
                      onClick={() => move(index, index - 1)}
                    />
                    <Button
                      type="text"
                      icon={<ArrowDownOutlined />}
                      aria-label={t('approvals.down')}
                      title={t('approvals.down')}
                      disabled={index === fields.length - 1}
                      onClick={() => move(index, index + 1)}
                    />
                    <Button
                      type="text"
                      danger
                      icon={<DeleteOutlined />}
                      aria-label={t('approvals.remove')}
                      title={t('approvals.remove')}
                      onClick={() => remove(field.name)}
                    />
                  </div>
                </div>
              );
            })}
            <Button
              type="dashed"
              icon={<PlusOutlined />}
              disabled={fields.length >= MAX_LEVELS}
              onClick={() => add({ kind: 'role' })}
            >
              {t('approvals.add')}
            </Button>
          </div>
        )}
      </Form.List>
    </DetailSection>
  );
}
