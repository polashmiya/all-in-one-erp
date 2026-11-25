import { Checkbox, Form, Switch } from 'antd';
import type { ReactNode } from 'react';
import { DetailSection } from '../data-display/DescriptionList';
import { FieldGrid, type FieldColumns } from './Field';

export function FieldSection({
  title,
  description,
  columns = 3,
  children,
}: {
  title: ReactNode;
  description?: ReactNode;
  columns?: FieldColumns;
  children: ReactNode;
}) {
  return (
    <DetailSection title={title} description={description}>
      <FieldGrid columns={columns}>{children}</FieldGrid>
    </DetailSection>
  );
}

export interface BooleanFieldOption {
  name: string;
  label: ReactNode;
}

export function CheckboxList({ options }: { options: BooleanFieldOption[] }) {
  return (
    <div className="erp-checklist">
      {options.map((option) => (
        <Form.Item key={option.name} name={option.name} valuePropName="checked" noStyle>
          <Checkbox>{option.label}</Checkbox>
        </Form.Item>
      ))}
    </div>
  );
}

export function SwitchList({ options }: { options: BooleanFieldOption[] }) {
  return (
    <div className="erp-switchlist">
      {options.map((option) => (
        <label key={option.name} className="erp-toggle-row">
          <span>{option.label}</span>
          <Form.Item name={option.name} valuePropName="checked">
            <Switch aria-label={typeof option.label === 'string' ? option.label : option.name} />
          </Form.Item>
        </label>
      ))}
    </div>
  );
}

export interface ChoiceOption<V extends string> {
  value: V;
  title: ReactNode;
  description?: ReactNode;
  icon?: ReactNode;
  badge?: ReactNode;
}

export function ChoiceCards<V extends string>({
  options,
  value,
  onChange,
  onConfirm,
  label,
}: {
  options: ChoiceOption<V>[];
  value?: V;
  onChange: (value: V) => void;
  onConfirm?: (value: V) => void;
  label: string;
}) {
  return (
    <div role="radiogroup" aria-label={label} className="erp-choices">
      {options.map((option) => (
        <button
          key={option.value}
          type="button"
          role="radio"
          aria-checked={option.value === value}
          className="erp-choice"
          onClick={() => onChange(option.value)}
          onDoubleClick={() => onConfirm?.(option.value)}
        >
          {option.icon && <span className="erp-choice__tile">{option.icon}</span>}
          <span className="erp-choice__text">
            <span className="erp-choice__title">{option.title}</span>
            {option.description && <span className="erp-choice__desc">{option.description}</span>}
          </span>
          {option.badge}
          <span className="erp-choice__radio" aria-hidden="true" />
        </button>
      ))}
    </div>
  );
}
