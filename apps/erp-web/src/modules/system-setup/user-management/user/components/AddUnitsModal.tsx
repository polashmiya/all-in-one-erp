import {
  applyFieldErrors,
  problemMessage,
  useAddUserUnits,
  type UserDetail,
} from '@erp/api-client';
import { FormModal } from '@erp/ui';
import { App, Form } from 'antd';
import { useTranslation } from 'react-i18next';
import { UnitChecklist } from './UnitChecklist';

type Values = { businessUnitIds: string[] };

export function AddUnitsModal({
  user,
  onClose,
  onDone,
}: {
  user?: UserDetail;
  onClose: () => void;
  onDone: () => unknown;
}) {
  const { t } = useTranslation();
  const { message } = App.useApp();
  const [form] = Form.useForm<Values>();
  const add = useAddUserUnits();

  const submit = async (values: Values) => {
    if (!user) return;
    try {
      await add.mutateAsync({ id: user.user.id, data: values });
      void message.success(t('user.units.added'));
      await onDone();
    } catch (error) {
      if (!applyFieldErrors(form, error))
        void message.error(problemMessage(error, t('user.failed')));
      throw error;
    }
  };

  return (
    <FormModal<Values>
      open={!!user}
      onClose={onClose}
      form={form}
      size="sm"
      title={t('user.units.title', { name: user?.user.fullName ?? '' })}
      subtitle={t('user.units.lead')}
      submitText={t('user.units.submit')}
      onSubmit={submit}
    >
      <UnitChecklist exclude={user?.units.map((unit) => unit.businessUnitId)} />
    </FormModal>
  );
}
