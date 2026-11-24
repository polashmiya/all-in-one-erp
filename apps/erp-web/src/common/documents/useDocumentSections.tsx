import type { ApprovalProgress } from '@erp/api-client';
import { DescriptionList, DetailSection, formatDateTime, type DetailsSection } from '@erp/ui';
import { useTranslation } from 'react-i18next';
import { ApprovalSteps } from '@/common/approval/ApprovalSteps';

interface RecordDates {
  createdAt: string | null;
  modifiedAt: string | null;
}

export function useDocumentSections(
  document: RecordDates,
  progress?: ApprovalProgress,
): DetailsSection[] {
  const { t } = useTranslation();

  const record: DetailsSection = {
    key: 'record',
    content: (
      <DetailSection title={t('document.side.recordInfo')}>
        <DescriptionList
          columns={3}
          items={[
            {
              key: 'created',
              label: t('document.side.created'),
              value: formatDateTime(document.createdAt),
            },
            {
              key: 'updated',
              label: t('document.side.updated'),
              value: document.modifiedAt ? formatDateTime(document.modifiedAt) : null,
            },
          ]}
        />
      </DetailSection>
    ),
  };

  return progress?.steps.length
    ? [{ key: 'approval', content: <ApprovalSteps steps={progress.steps} /> }, record]
    : [record];
}
