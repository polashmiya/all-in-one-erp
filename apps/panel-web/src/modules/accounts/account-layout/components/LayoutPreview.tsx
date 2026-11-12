import { usePreviewFeatureConfiguration, type FeatureConfig } from '@erp/api-client';
import { useFormSections } from '@erp/metadata-engine';
import { AppDrawer } from '@erp/ui';
import { Form } from 'antd';

function PreviewForm({ config }: { config: FeatureConfig }) {
  const { sections } = useFormSections(config);

  return (
    <Form layout="vertical" className="erp-form" disabled>
      <div className="erp-form-stack">
        {sections.map((section) => (
          <section key={section.key} className="erp-card erp-form-block">
            {section.content}
          </section>
        ))}
      </div>
    </Form>
  );
}

export function LayoutPreview({
  accountId,
  feature,
  businessUnitId,
  open,
  title,
  subtitle,
  onClose,
}: {
  accountId: string;
  feature: string;
  businessUnitId?: string;
  open: boolean;
  title: string;
  subtitle: string;
  onClose: () => void;
}) {
  const preview = usePreviewFeatureConfiguration(
    accountId,
    feature,
    businessUnitId ? { businessUnitId } : undefined,
    { query: { enabled: open && !!feature, staleTime: 0 } },
  );

  return (
    <AppDrawer
      open={open}
      onClose={onClose}
      size="lg"
      title={title}
      subtitle={subtitle}
      loading={preview.isFetching}
    >
      {preview.data && <PreviewForm config={preview.data} />}
    </AppDrawer>
  );
}
