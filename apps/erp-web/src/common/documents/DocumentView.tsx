import { EditOutlined } from '@ant-design/icons';
import type { FeatureConfig } from '@erp/api-client';
import { useViewTabs } from '@erp/metadata-engine';
import { DetailsPageLayout, StatusTag, type RecordMeta } from '@erp/ui';
import { useQuery } from '@tanstack/react-query';
import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router';
import { useCustomLookups } from '@/common/hooks/useCustomLookups';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { useUnitKey } from '@/common/master-data';
import type { DocumentPaths } from './documents';
import { ApprovalStatusTag } from './ApprovalStatusTag';
import { useApprovalActions, type ApprovalCalls } from './useApprovalActions';
import { useDocumentSections } from './useDocumentSections';

interface ViewDocument {
  id: number;
  code: string;
  status: string;
  createdAt: string | null;
  modifiedAt: string | null;
}

export function DocumentView<T extends ViewDocument>({
  document,
  config,
  feature,
  paths,
  calls,
  icon,
  tag,
  meta,
  lines,
  renderers,
}: {
  document: T;
  config: FeatureConfig;
  feature: string;
  paths: DocumentPaths;
  calls: ApprovalCalls;
  icon: ReactNode;
  tag?: string | null;
  meta: RecordMeta[];
  lines: ReactNode;
  renderers?: Record<string, (record: T) => ReactNode>;
}) {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const access = useFeatureAccess(feature);
  const tabs = useViewTabs(config, document, renderers, useCustomLookups(config));
  const progress = useQuery({
    queryKey: useUnitKey(calls.progressKey(document.id)),
    queryFn: () => calls.progress(document.id),
  });
  const sections = useDocumentSections(document, progress.data);
  const { actions, modal } = useApprovalActions(feature, calls, (action) => {
    if (action === 'deleted') void navigate(paths.list, { replace: true });
  });
  const [approve, ...more] = actions(document, !!progress.data?.canAct);
  const extra = [{ key: 'lines', content: lines }, ...sections];

  return (
    <>
      <DetailsPageLayout
        crumbs={[document.code]}
        header={{
          backTo: paths.list,
          icon,
          tint: 'blue',
          title: document.code,
          tags: (
            <>
              <ApprovalStatusTag status={document.status} />
              {tag && <StatusTag tone="violet">{tag}</StatusTag>}
            </>
          ),
          meta: meta.filter((item) => item.label),
          actions: [
            {
              key: 'edit',
              label: t('master.edit'),
              icon: <EditOutlined />,
              variant: 'primary',
              hidden: !access.edit || document.status !== 'pending',
              to: paths.edit(document.id),
            },
            ...(approve ? [approve] : []),
            {
              key: 'more',
              label: t('common.moreActions'),
              hidden: more.every((action) => action.hidden),
              menu: more,
            },
          ],
        }}
        tabs={tabs.map((tab, index) =>
          index === tabs.length - 1 ? { ...tab, sections: [...tab.sections, ...extra] } : tab,
        )}
      />
      {modal}
    </>
  );
}
