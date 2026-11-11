import type { ReactNode } from 'react';
import type { ActionItem } from '../../components/actions/types';
import { RecordHeader, type RecordHeaderProps } from '../../components/data-display/RecordHeader';
import { PageState } from '../../components/feedback/PageState';
import { PageContainer } from '../page/PageContainer';

export interface DetailsSection {
  key: string;
  content: ReactNode;
}

export interface DetailsPageLayoutProps {
  loading?: boolean;
  missing?: boolean;
  missingTitle?: ReactNode;
  missingActions?: ActionItem[];
  crumbs?: string[];
  header?: RecordHeaderProps;
  sections?: DetailsSection[];
  children?: ReactNode;
}

export function DetailsPageLayout({
  loading,
  missing,
  missingTitle,
  missingActions,
  crumbs,
  header,
  sections = [],
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
          <div className="erp-form-stack">
            {sections.map((section) => (
              <section key={section.key} className="erp-card erp-form-block">
                {section.content}
              </section>
            ))}
            {children}
          </div>
        </div>
      </PageContainer>
    </PageState>
  );
}
