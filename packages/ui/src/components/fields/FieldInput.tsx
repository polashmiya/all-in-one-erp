import { Checkbox, DatePicker, Input, InputNumber, Radio, Segmented, Select } from 'antd';
import type { ReactNode } from 'react';
import { DATE_FORMAT, DATE_TIME_FORMAT } from '../../utils/format';
import { MoneyInput, NumberStepper, PercentInput, YesNoSwitch } from './inputs';
import { LookupSelect, type LookupOption } from './LookupSelect';

export type FieldDataType =
  | 'text'
  | 'textarea'
  | 'number'
  | 'decimal'
  | 'money'
  | 'percent'
  | 'date'
  | 'datetime'
  | 'dateRange'
  | 'boolean'
  | 'select'
  | 'multiSelect'
  | 'lookup';

export type FieldWidget = 'switch' | 'checkbox' | 'radio' | 'segmented' | 'dropdown';

export interface FieldOption {
  value: string;
  label: ReactNode;
}

export interface FieldInputProps {
  dataType: FieldDataType;
  widget?: FieldWidget;
  options?: FieldOption[];
  fetchOptions?: (query: string) => Promise<LookupOption[]>;
  placeholder?: string;
  maxLength?: number;
  disabled?: boolean;
  readOnly?: boolean;
  value?: unknown;
  onChange?: (value: unknown) => void;
  id?: string;
}

export function FieldInput({
  dataType,
  widget,
  options = [],
  fetchOptions,
  placeholder,
  maxLength,
  disabled,
  readOnly,
  value,
  onChange,
  id,
}: FieldInputProps) {
  const common = { id, disabled: disabled || readOnly, placeholder };
  const v = value as never;
  const change = onChange as never;

  switch (dataType) {
    case 'textarea':
      return (
        <Input.TextArea
          {...common}
          value={v}
          onChange={(e) => onChange?.(e.target.value)}
          maxLength={maxLength}
          autoSize={{ minRows: 1, maxRows: 4 }}
        />
      );
    case 'number':
      return <NumberStepper {...common} value={v} onChange={change} />;
    case 'decimal':
      return (
        <InputNumber<number>
          {...common}
          value={v}
          onChange={change}
          precision={2}
          controls={false}
          className="erp-w-full"
        />
      );
    case 'money':
      return <MoneyInput {...common} value={v} onChange={change} className="erp-w-full" />;
    case 'percent':
      return <PercentInput {...common} value={v} onChange={change} className="erp-w-full" />;
    case 'date':
      return (
        <DatePicker
          {...common}
          value={v}
          onChange={change}
          format={DATE_FORMAT}
          className="erp-w-full"
        />
      );
    case 'datetime':
      return (
        <DatePicker
          {...common}
          value={v}
          onChange={change}
          showTime={{ format: 'HH:mm' }}
          format={DATE_TIME_FORMAT}
          className="erp-w-full"
        />
      );
    case 'dateRange':
      return (
        <DatePicker.RangePicker
          id={id}
          disabled={disabled || readOnly}
          value={v}
          onChange={change}
          format={DATE_FORMAT}
          className="erp-w-full"
        />
      );
    case 'boolean':
      return widget === 'checkbox' ? (
        <Checkbox
          id={id}
          disabled={disabled || readOnly}
          checked={!!value}
          onChange={(e) => onChange?.(e.target.checked)}
        />
      ) : (
        <YesNoSwitch
          id={id}
          disabled={disabled || readOnly}
          checked={!!value}
          onChange={(checked) => onChange?.(checked)}
        />
      );
    case 'select':
      if (widget === 'radio')
        return (
          <Radio.Group
            id={id}
            disabled={disabled || readOnly}
            value={v}
            onChange={(e) => onChange?.(e.target.value)}
            options={options.map((o) => ({ value: o.value, label: o.label }))}
          />
        );
      if (widget === 'segmented')
        return (
          <Segmented
            id={id}
            disabled={disabled || readOnly}
            value={v}
            onChange={change}
            options={options.map((o) => ({ value: o.value, label: o.label }))}
          />
        );
      return (
        <Select
          {...common}
          value={v}
          onChange={change}
          options={options}
          allowClear
          showSearch
          optionFilterProp="label"
        />
      );
    case 'multiSelect':
      return (
        <Select
          {...common}
          mode="multiple"
          value={v}
          onChange={change}
          options={options}
          allowClear
          optionFilterProp="label"
        />
      );
    case 'lookup':
      return fetchOptions ? (
        <LookupSelect {...common} value={v} onChange={change} fetchOptions={fetchOptions} />
      ) : (
        <Select {...common} value={v} onChange={change} options={options} showSearch />
      );
    case 'text':
    default:
      return (
        <Input
          {...common}
          value={v}
          onChange={(e) => onChange?.(e.target.value)}
          maxLength={maxLength}
        />
      );
  }
}
