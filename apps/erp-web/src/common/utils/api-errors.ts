import { ApiError } from '@erp/api-client';
import type { FormInstance } from 'antd';

export function applyFieldErrors(form: FormInstance, error: unknown): boolean {
  if (!(error instanceof ApiError) || error.status !== 400) return false;

  const known = new Set(Object.keys(form.getFieldsValue(true) as object));
  const fields = Object.entries(error.fieldErrors)
    .map(([name, errors]) => ({ name: name.split('.'), errors }))
    .filter(({ name }) => known.has(name[0] ?? ''));

  if (fields.length > 0) form.setFields(fields);
  return fields.length > 0;
}

export function problemMessage(error: unknown, fallback: string): string {
  if (!(error instanceof ApiError)) return fallback;
  return error.problem?.detail ?? Object.values(error.fieldErrors)[0]?.[0] ?? fallback;
}
