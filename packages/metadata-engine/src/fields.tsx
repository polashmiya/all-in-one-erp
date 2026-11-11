import type { ConfigField } from '@erp/api-client';
import { Field, FieldInput, useLanguageStore, type FieldDataType } from '@erp/ui';
import { Form, Select, type SelectProps } from 'antd';
import { use } from 'react';
import { columnsOf, useText, type FieldValues } from './config';
import { Engine, type FieldState } from './context';
import { childSelectOptions, localized, parentOf, selectOptions, type LookupItem } from './lookups';
import { useFieldRules } from './rules';

const NO_STATE: FieldState = {};

export function DynamicField({ field }: { field: ConfigField }) {
  const behavior = use(Engine).behaviors?.[field.key];
  if (!behavior) return <FieldItem field={field} state={NO_STATE} />;

  return (
    <Form.Item noStyle dependencies={behavior.watch}>
      {(form) => (
        <FieldItem field={field} state={behavior.state(form.getFieldsValue(true) as FieldValues)} />
      )}
    </Form.Item>
  );
}

function FieldItem({ field, state }: { field: ConfigField; state: FieldState }) {
  const text = useText();
  const rules = useFieldRules();
  const widget = use(Engine).widgets?.[field.key];
  if (state.hidden) return null;

  const label = text(field.label);
  const disabled = !!state.disabled || field.readOnly;
  const required = !disabled && (field.required || !!state.required);
  const placeholder = state.placeholder ?? text(field.placeholder);

  return (
    <Field
      name={field.key}
      label={label}
      extra={text(field.help)}
      rules={rules(field, label, required)}
      className={field.colSpan > 1 ? `erp-field-span-${columnsOf(field.colSpan)}` : undefined}
    >
      {widget ? (
        widget(field)
      ) : field.type === 'lookup' ? (
        <LookupInput
          field={field}
          filter={state.filter}
          disabled={disabled}
          placeholder={placeholder}
          allowClear={!required}
        />
      ) : (
        <FieldInput
          dataType={field.type as FieldDataType}
          placeholder={placeholder}
          maxLength={field.maxLength ?? undefined}
          disabled={disabled}
        />
      )}
    </Field>
  );
}

function ancestorsOf(fields: Map<string, ConfigField>, key: string) {
  const keys: string[] = [];
  for (let parent = fields.get(key)?.parentField; parent && fields.has(parent);) {
    keys.push(parent);
    parent = fields.get(parent)?.parentField;
  }
  return keys;
}

function LookupInput({
  field,
  filter,
  disabled,
  placeholder,
  allowClear,
  id,
  value,
  onChange,
}: {
  field: ConfigField;
  filter?: (item: LookupItem) => boolean;
  disabled: boolean;
  placeholder?: string;
  allowClear: boolean;
  id?: string;
  value?: unknown;
  onChange?: (value: unknown) => void;
}) {
  const { fields, lookups = {} } = use(Engine);
  const language = useLanguageStore((s) => s.language);
  const form = Form.useFormInstance();
  const ancestors = ancestorsOf(fields, field.key);
  const chain =
    Form.useWatch((values: FieldValues) => ancestors.map((key) => values[key]), form) ?? [];
  const valueOf = (key: string) => chain[ancestors.indexOf(key)];

  const allowed = (key: string): LookupItem[] | undefined => {
    const own = fields.get(key);
    const items = own?.lookup ? localized(lookups[own.lookup], language) : undefined;
    if (!own?.parentField || !fields.has(own.parentField)) return items;
    const parentValue = valueOf(own.parentField);
    if (parentValue != null) return items?.filter((item) => item.parentId === parentValue);
    const parents = allowed(own.parentField);
    return parents ? items?.filter((item) => parents.some((p) => p.id === item.parentId)) : items;
  };

  const items = filter ? allowed(field.key)?.filter(filter) : allowed(field.key);
  const parentKey = ancestors[0];
  const options: SelectProps['options'] =
    parentKey && valueOf(parentKey) == null
      ? childSelectOptions(items, allowed(parentKey), null, value)
      : selectOptions(items, value);

  const change = (next: unknown) => {
    onChange?.(next);
    const updates: FieldValues = {};
    let id = next;
    for (let key = field.key; id != null;) {
      const own = fields.get(key);
      if (!own?.parentField || !fields.has(own.parentField)) break;
      id = parentOf(own.lookup ? lookups[own.lookup] : undefined, id);
      updates[own.parentField] = id;
      key = own.parentField;
    }
    const clear = (parent: string) => {
      for (const child of fields.values()) {
        if (child.parentField !== parent) continue;
        updates[child.key] = undefined;
        clear(child.key);
      }
    };
    clear(field.key);
    form.setFieldsValue(updates);
  };

  return (
    <Select
      id={id}
      value={value ?? undefined}
      onChange={change}
      options={options}
      showSearch
      optionFilterProp="label"
      allowClear={allowClear}
      disabled={disabled}
      placeholder={placeholder}
    />
  );
}
