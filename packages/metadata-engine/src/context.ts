import type { ConfigField, ItemLookup } from '@erp/api-client';
import { createContext, type ReactElement } from 'react';
import type { FieldValues } from './config';
import type { LookupSources } from './lookups';

export interface FieldState {
  hidden?: boolean;
  disabled?: boolean;
  required?: boolean;
  placeholder?: string;
  filter?: (item: ItemLookup) => boolean;
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
}

export interface EngineContext extends FormOptions {
  fields: Map<string, ConfigField>;
}

export const Engine = createContext<EngineContext>({ fields: new Map() });
