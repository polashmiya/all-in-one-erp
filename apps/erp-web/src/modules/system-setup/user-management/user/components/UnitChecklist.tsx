import { useGetUserUnitChoices } from '@erp/api-client';
import { Checkbox, Form } from 'antd';
import { useTranslation } from 'react-i18next';

export function UnitChecklist({ exclude = [] }: { exclude?: string[] }) {
  const { t } = useTranslation();
  const units = useGetUserUnitChoices();

  return (
    <Form.Item
      name="businessUnitIds"
      label={t('user.units.label')}
      extra={t('user.units.hint')}
      rules={[{ required: true, type: 'array', min: 1, message: t('user.units.required') }]}
    >
      <Checkbox.Group
        className="erp-stack"
        options={(units.data ?? [])
          .filter((unit) => !exclude.includes(unit.id))
          .map((unit) => ({
            value: unit.id,
            label: unit.canAdd ? unit.name : t('user.units.noPermission', { name: unit.name }),
            disabled: !unit.canAdd,
          }))}
      />
    </Form.Item>
  );
}
