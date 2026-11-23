import { StatusTag } from '@erp/ui';
import { useTranslation } from 'react-i18next';
import { STATUS_TONES } from '../constants';

export function RequisitionStatusTag({ status }: { status: string }) {
  const { t } = useTranslation();
  return (
    <StatusTag tone={STATUS_TONES[status] ?? 'neutral'} dot>
      {t(`requisition.status.${status}`)}
    </StatusTag>
  );
}
