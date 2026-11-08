import type { ConfigField } from '@erp/api-client';
import type { FormRule } from 'antd';
import { useTranslation } from 'react-i18next';
import { isText } from './config';

export function useFieldRules() {
  const { t } = useTranslation();

  return (field: ConfigField, label: string | undefined, required: boolean): FormRule[] => {
    const rules: FormRule[] = [];
    if (required && field.type !== 'boolean')
      rules.push({
        required: true,
        whitespace: isText(field),
        message: t('field.required', { label }),
      });
    if (field.maxLength != null && isText(field))
      rules.push({
        max: field.maxLength,
        message: t('field.maxLength', { count: field.maxLength }),
      });
    if (field.min != null)
      rules.push({ type: 'number', min: field.min, message: t('field.min', { min: field.min }) });
    if (field.max != null)
      rules.push({ type: 'number', max: field.max, message: t('field.max', { max: field.max }) });
    return rules;
  };
}
