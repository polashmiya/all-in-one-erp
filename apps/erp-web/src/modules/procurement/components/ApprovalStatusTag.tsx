import { StatusTag } from '@erp/ui';
import { useTranslation } from 'react-i18next';
import { STATUS_TONES } from '../procurement';

export function ApprovalStatusTag({ status }: { status: string }) {
  const { t } = useTranslation();
  return (
    <StatusTag tone={STATUS_TONES[status] ?? 'neutral'} dot>
      {t(`procurement.status.${status}`)}
    </StatusTag>
  );
}
