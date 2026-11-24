import { problemMessage, setFiscalYearStart } from '@erp/api-client';
import { Field, formatDate, FormModal } from '@erp/ui';
import { App, Select } from 'antd';
import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { useRefreshDocuments } from '@/common/documents/documents';
import { MONTHS } from '../constants';

export function FiscalYearModal({
  open,
  startMonth,
  onClose,
}: {
  open: boolean;
  startMonth: number;
  onClose: () => void;
}) {
  const { t } = useTranslation();
  const { message } = App.useApp();
  const refresh = useRefreshDocuments();
  const initialValues = useMemo(() => ({ startMonth }), [startMonth]);

  const save = async (values: { startMonth: number }) => {
    try {
      await setFiscalYearStart(values);
      await refresh();
      void message.success(t('period.done.start'));
    } catch (error) {
      void message.error(problemMessage(error, t('master.failed')));
      throw error;
    }
  };

  return (
    <FormModal<{ startMonth: number }>
      open={open}
      onClose={onClose}
      title={t('period.fiscalYear')}
      subtitle={t('period.fiscalYearText')}
      submitText={t('master.save')}
      initialValues={initialValues}
      onSubmit={save}
    >
      <Field name="startMonth" label={t('period.startMonth')} rules={[{ required: true }]}>
        <Select
          options={MONTHS.map((month) => ({
            value: month,
            label: formatDate(new Date(2000, month - 1, 1), 'MMMM'),
          }))}
        />
      </Field>
    </FormModal>
  );
}
