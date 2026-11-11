import type { ConfigSection, FeatureConfig } from '@erp/api-client';
import { FieldSection, type FormSection } from '@erp/ui';
import { columnsOf, fieldMap, useText } from './config';
import { Engine, type EngineContext, type FormOptions } from './context';
import { DynamicField } from './fields';

const MAIN_COLUMNS = 3;
const ASIDE_COLUMNS = 1;

export function useFormSections(config: FeatureConfig, options: FormOptions = {}) {
  const text = useText();
  const context: EngineContext = { ...options, fields: fieldMap(config) };
  const asideKeys = new Set(options.aside ?? []);
  const all = config.tabs.flatMap((tab) => tab.sections);

  const toSection = (section: ConfigSection, columns: 1 | 2 | 3 | 4): FormSection => ({
    key: section.key,
    fields: section.fields.map((field) => field.key),
    content: (
      <Engine value={context}>
        <FieldSection title={text(section.label)} columns={columns}>
          {section.fields.map((field) => (
            <DynamicField key={field.key} field={field} />
          ))}
        </FieldSection>
      </Engine>
    ),
  });

  return {
    sections: all
      .filter((section) => !asideKeys.has(section.key))
      .map((section) => toSection(section, columnsOf(Math.min(section.columns, MAIN_COLUMNS)))),
    aside: all
      .filter((section) => asideKeys.has(section.key))
      .map((section) => toSection(section, ASIDE_COLUMNS)),
  };
}
