import type { FeatureConfig } from '@erp/api-client';
import { FieldSection, type FormTab } from '@erp/ui';
import { columnsOf, fieldMap, useText } from './config';
import { Engine, type EngineContext, type FormOptions } from './context';
import { DynamicField } from './fields';

export function useFormTabs(config: FeatureConfig, options: FormOptions = {}): FormTab[] {
  const text = useText();
  const context: EngineContext = { ...options, fields: fieldMap(config) };

  return config.tabs.map((tab) => ({
    key: tab.key,
    label: text(tab.label),
    fields: tab.sections.flatMap((section) => section.fields.map((field) => field.key)),
    content: (
      <Engine value={context}>
        {tab.sections.map((section) => (
          <FieldSection
            key={section.key}
            title={text(section.label)}
            columns={columnsOf(section.columns)}
          >
            {section.fields.map((field) => (
              <DynamicField key={field.key} field={field} />
            ))}
          </FieldSection>
        ))}
      </Engine>
    ),
  }));
}
