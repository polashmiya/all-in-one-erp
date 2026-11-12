import type { ConfigSection, FeatureConfig } from '@erp/api-client';
import { FieldGrid, FieldSection } from '@erp/ui';
import { Tabs } from 'antd';
import { columnsOf, fieldMap, useText } from './config';
import { Engine, type EngineContext, type FormOptions } from './context';
import { DynamicField } from './fields';

export function ConfigFields({ config, ...options }: { config: FeatureConfig } & FormOptions) {
  const text = useText();
  const context: EngineContext = { ...options, fields: fieldMap(config) };
  const titled = config.tabs.flatMap((tab) => tab.sections).length > 1;

  const body = (sections: ConfigSection[]) => (
    <Engine value={context}>
      {sections.map((section) => {
        const fields = section.fields.map((field) => (
          <DynamicField key={field.key} field={field} />
        ));
        const columns = columnsOf(section.columns);
        return titled ? (
          <FieldSection key={section.key} title={text(section.label)} columns={columns}>
            {fields}
          </FieldSection>
        ) : (
          <FieldGrid key={section.key} columns={columns}>
            {fields}
          </FieldGrid>
        );
      })}
    </Engine>
  );

  if (config.tabs.length < 2) return body(config.tabs.flatMap((tab) => tab.sections));

  return (
    <Tabs
      items={config.tabs.map((tab) => ({
        key: tab.key,
        label: text(tab.label),
        forceRender: true,
        children: body(tab.sections),
      }))}
    />
  );
}
