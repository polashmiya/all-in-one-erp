import { DescriptionList, DetailSection, formatDateTime, type DetailsSection } from '@erp/ui';
import { useTranslation } from 'react-i18next';

interface ReviewedDocument {
  status: string;
  reviewedAt: string | null;
  reviewedByName: string | null;
  reviewComment: string | null;
  createdAt: string | null;
  modifiedAt: string | null;
}

export function useDocumentSections(document: ReviewedDocument): DetailsSection[] {
  const { t } = useTranslation();

  const review: DetailsSection = {
    key: 'review',
    content: (
      <DetailSection title={t('procurement.side.review')}>
        <DescriptionList
          columns={3}
          items={[
            { key: 'by', label: t('procurement.side.reviewedBy'), value: document.reviewedByName },
            {
              key: 'at',
              label: t('procurement.side.reviewedAt'),
              value: formatDateTime(document.reviewedAt),
            },
            {
              key: 'comment',
              label: t(
                document.status === 'rejected'
                  ? 'procurement.review.reason'
                  : 'procurement.review.comment',
              ),
              value: document.reviewComment,
              wide: true,
            },
          ]}
        />
      </DetailSection>
    ),
  };

  const record: DetailsSection = {
    key: 'record',
    content: (
      <DetailSection title={t('procurement.side.recordInfo')}>
        <DescriptionList
          columns={3}
          items={[
            {
              key: 'created',
              label: t('procurement.side.created'),
              value: formatDateTime(document.createdAt),
            },
            {
              key: 'updated',
              label: t('procurement.side.updated'),
              value: document.modifiedAt ? formatDateTime(document.modifiedAt) : null,
            },
          ]}
        />
      </DetailSection>
    ),
  };

  return document.reviewedAt ? [review, record] : [record];
}
