import { BranchesOutlined } from '@ant-design/icons';
import { Form, type FormItemProps } from 'antd';
import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { StatusTag } from '../data-display/StatusTag';

export interface FieldProps extends FormItemProps {
  custom?: boolean;
  rule?: ReactNode;
}

export function Field({ label, custom = false, rule, ...props }: FieldProps) {
  const { t } = useTranslation();

  const decorated =
    label && (custom || rule) ? (
      <span className="erp-field-label">
        {label}
        {custom && <StatusTag tone="violet">{t('field.custom')}</StatusTag>}
        {rule && (
          <span className="erp-field-rule">
            <BranchesOutlined />
            {rule}
          </span>
        )}
      </span>
    ) : (
      label
    );

  return <Form.Item label={decorated} {...props} />;
}

export type FieldColumns = 1 | 2 | 3 | 4 | 6;

export function FieldGrid({ columns, children }: { columns?: FieldColumns; children: ReactNode }) {
  return (
    <div className={columns ? `erp-field-grid erp-field-grid--${columns}` : 'erp-field-grid'}>
      {children}
    </div>
  );
}
