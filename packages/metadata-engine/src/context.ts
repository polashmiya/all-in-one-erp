import type { ConfigField } from '@erp/api-client';
import { createContext, type ReactElement } from 'react';
import type { FieldValues } from './config';
import type { LookupItem, LookupSources } from './lookups';

export interface FieldState {
  hidden?: boolean;
  disabled?: boolean;
  required?: boolean;
  placeholder?: string;
  filter?: (item: LookupItem) => boolean;
}

export interface FieldBehavior {
  watch: string[];
  state: (values: FieldValues) => FieldState;
}

export type FieldRenderer = (field: ConfigField) => ReactElement;

export interface FormOptions {
  lookups?: LookupSources;
  widgets?: Record<string, FieldRenderer>;
  behaviors?: Record<string, FieldBehavior>;
  aside?: string[];
}

export interface EngineContext extends FormOptions {
  fields: Map<string, ConfigField>;
}

export const Engine = createContext<EngineContext>({ fields: new Map() });
