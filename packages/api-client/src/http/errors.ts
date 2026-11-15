import { ApiError } from './client';

export interface FieldErrorForm {
  getFieldsValue: (all: true) => unknown;
  setFields: (fields: never) => void;
}

export function applyFieldErrors(form: FieldErrorForm, error: unknown): boolean {
  if (!(error instanceof ApiError) || error.status !== 400) return false;

  const known = new Set(Object.keys(form.getFieldsValue(true) as object));
  const fields = Object.entries(error.fieldErrors)
    .map(([name, errors]) => ({ name: name.split('.'), errors }))
    .filter(({ name }) => known.has(name[0] ?? ''));

  if (fields.length > 0) form.setFields(fields as never);
  return fields.length > 0;
}

export function problemMessage(error: unknown, fallback: string): string {
  if (!(error instanceof ApiError)) return fallback;
  return error.problem?.detail ?? Object.values(error.fieldErrors)[0]?.[0] ?? fallback;
}
