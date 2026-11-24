import {
  useCreateBankAccount,
  useUpdateBankAccount,
  type BankAccountRow,
  type FeatureConfig,
  type SaveBankAccountRequest,
} from '@erp/api-client';
import type { LookupItem } from '@erp/metadata-engine';
import { Form } from 'antd';
import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { MasterFormModal, trimmed } from '@/common/master-data';
import { useAccountOptions } from '../../../accounting';
import { BANK_LOOKUPS } from '../constants';

export function BankAccountFormModal({
  open,
  bank,
  types,
  config,
  unitName,
  onClose,
  onSaved,
}: {
  open: boolean;
  bank?: BankAccountRow;
  types: LookupItem[];
  config: FeatureConfig;
  unitName: string;
  onClose: () => void;
  onSaved: () => Promise<unknown>;
}) {
  const { t } = useTranslation();
  const [form] = Form.useForm<SaveBankAccountRequest>();
  const accounts = useAccountOptions(open);
  const create = useCreateBankAccount();
  const update = useUpdateBankAccount();
  const initialValues = useMemo<Partial<SaveBankAccountRequest>>(() => bank ?? {}, [bank]);

  const save = async (values: SaveBankAccountRequest) => {
    const data: SaveBankAccountRequest = {
      name: values.name.trim(),
      type: values.type,
      bankName: trimmed(values.bankName),
      branchName: trimmed(values.branchName),
      accountNo: values.accountNo.trim(),
      routingNo: trimmed(values.routingNo),
      chartOfAccountId: values.chartOfAccountId ?? null,
      isActive: values.isActive ?? true,
      customData: values.customData ?? null,
    };
    const saved = bank
      ? await update.mutateAsync({ id: bank.id, data })
      : await create.mutateAsync({ data });
    await onSaved();
    return saved;
  };

  return (
    <MasterFormModal<SaveBankAccountRequest>
      open={open}
      onClose={onClose}
      title={t(bank ? 'bankAccount.form.editTitle' : 'bankAccount.form.newTitle')}
      subtitle={t('bankAccount.form.subtitle', { unit: unitName })}
      form={form}
      config={config}
      options={{
        lookups: {
          [BANK_LOOKUPS.type]: types,
          [BANK_LOOKUPS.account]: accounts?.filter((a) => !a.isGroup && a.type === 'currentAsset'),
        },
      }}
      initialValues={initialValues}
      editing={!!bank}
      onSave={save}
    />
  );
}
