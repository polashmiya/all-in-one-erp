import type { ReactNode } from 'react';
import type { ActionItem } from '../../components/actions/types';
import { RecordHeader, type RecordHeaderProps } from '../../components/data-display/RecordHeader';
import { MiniStatGrid, type MiniStatItem } from '../../components/data-display/StatGrid';
import { PageState } from '../../components/feedback/PageState';
import { PageContainer } from '../page/PageContainer';
import { SplitLayout } from '../page/CardGrid';
import { SectionTabs, type SectionTab } from './SectionTabs';

export type DetailsTab = SectionTab;

export interface DetailsPageLayoutProps {
  loading?: boolean;
  missing?: boolean;
  missingTitle?: ReactNode;
  missingActions?: ActionItem[];
  crumbs?: string[];
  header?: RecordHeaderProps;
  stats?: MiniStatItem[];
  tabs?: DetailsTab[];
  children?: ReactNode;
  aside?: ReactNode;
}

export function DetailsPageLayout({
  loading,
  missing,
  missingTitle,
  missingActions,
  crumbs,
  header,
  stats,
  tabs,
  children,
  aside,
}: DetailsPageLayoutProps) {
  return (
    <PageState
      loading={loading}
      missing={missing}
      missingTitle={missingTitle}
      missingActions={missingActions}
    >
      <PageContainer title={false} crumbs={crumbs}>
        {header && <RecordHeader {...header} />}
        {stats && stats.length > 0 && <MiniStatGrid items={stats} />}
        <SplitLayout
          aside={aside}
          main={
            <section className={tabs ? 'erp-card erp-form-card' : 'erp-card'}>
              {tabs ? <SectionTabs tabs={tabs} /> : children}
            </section>
          }
        />
      </PageContainer>
    </PageState>
  );
}
