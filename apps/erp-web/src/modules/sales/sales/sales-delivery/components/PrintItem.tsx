import type { SalesDeliveryLineDetail } from '@erp/api-client';
import { useTranslation } from 'react-i18next';

export function PrintItem({ line }: { line: SalesDeliveryLineDetail }) {
  const { t } = useTranslation();
  const details = [
    line.itemCode,
    line.itemDescription,
    line.batch && t('print.batch', { value: line.batch }),
    line.itemSerial && t('print.serial', { value: line.itemSerial }),
    line.warrantyInMonth > 0 && t('print.warranty', { count: line.warrantyInMonth }),
  ];

  return (
    <>
      <strong>{line.itemName}</strong>
      <p className="erp-print__muted">{details.filter(Boolean).join(' · ')}</p>
    </>
  );
}
