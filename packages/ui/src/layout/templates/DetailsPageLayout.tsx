import type { ReactNode } from 'react';
import type { ActionItem } from '../../components/actions/types';
import { RecordHeader, type RecordHeaderProps } from '../../components/data-display/RecordHeader';
import { PageState } from '../../components/feedback/PageState';
import { PageContainer } from '../page/PageContainer';
import { SectionTabs } from './SectionTabs';

export interface DetailsSection {
  key: string;
  content: ReactNode;
}

export interface DetailsTab {
  key: string;
  label: ReactNode;
  sections: DetailsSection[];
}

export interface DetailsPageLayoutProps {
  loading?: boolean;
  missing?: boolean;
  missingTitle?: ReactNode;
  missingActions?: ActionItem[];
  crumbs?: string[];
  header?: RecordHeaderProps;
  tabs?: DetailsTab[];
  children?: ReactNode;
}

function Cards({ sections }: { sections: DetailsSection[] }) {
  return (
    <div className="erp-form-stack">
      {sections.map((section) => (
        <section key={section.key} className="erp-card erp-form-block">
          {section.content}
        </section>
      ))}
    </div>
  );
}

export function DetailsPageLayout({
  loading,
  missing,
  missingTitle,
  missingActions,
  crumbs,
  header,
  tabs = [],
  children,
}: DetailsPageLayoutProps) {
  return (
    <PageState
      loading={loading}
      missing={missing}
      missingTitle={missingTitle}
      missingActions={missingActions}
    >
      <PageContainer title={false} crumbs={crumbs}>
        <div className="erp-details">
          {header && <RecordHeader {...header} />}
          <div className="erp-details__main">
            {tabs.length > 1 ? (
              <SectionTabs
                tabs={tabs.map((tab) => ({
                  key: tab.key,
                  label: tab.label,
                  content: <Cards sections={tab.sections} />,
                }))}
              />
            ) : (
              <Cards sections={tabs.flatMap((tab) => tab.sections)} />
            )}
            {children}
          </div>
        </div>
      </PageContainer>
    </PageState>
  );
}
