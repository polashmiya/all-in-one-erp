import {
  useCreateChartOfAccount,
  useUpdateChartOfAccount,
  type ChartOfAccountOption,
  type ChartOfAccountRow,
  type FeatureConfig,
  type SaveChartOfAccountRequest,
} from '@erp/api-client';
import { Form } from 'antd';
import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { MasterFormModal, trimmed } from '@/common/master-data';
import { useAccountTypes } from '../../../accounting';
import { CHART_LOOKUPS } from '../constants';

export function AccountFormModal({
  open,
  account,
  accounts,
  config,
  accountName,
  onClose,
  onSaved,
}: {
  open: boolean;
  account?: ChartOfAccountRow;
  accounts?: ChartOfAccountOption[];
  config: FeatureConfig;
  accountName: string;
  onClose: () => void;
  onSaved: () => Promise<unknown>;
}) {
  const { t } = useTranslation();
  const [form] = Form.useForm<SaveChartOfAccountRequest>();
  const types = useAccountTypes();
  const create = useCreateChartOfAccount();
  const update = useUpdateChartOfAccount();
  const initialValues = useMemo<Partial<SaveChartOfAccountRequest>>(() => account ?? {}, [account]);
  const isMain = !!account && !account.parentId;
  const groups = accounts?.filter((a) => a.isGroup && a.id !== account?.id);

  const save = async (values: SaveChartOfAccountRequest) => {
    const data: SaveChartOfAccountRequest = {
      code: values.code.trim(),
      name: values.name.trim(),
      type: values.type,
      isGroup: !!values.isGroup,
      parentId: values.parentId ?? null,
      description: trimmed(values.description),
      isActive: values.isActive ?? true,
      customData: values.customData ?? null,
    };
    const saved = account
      ? await update.mutateAsync({ id: account.id, data })
      : await create.mutateAsync({ data });
    await onSaved();
    return saved;
  };

  return (
    <MasterFormModal<SaveChartOfAccountRequest>
      open={open}
      onClose={onClose}
      title={t(account ? 'chart.form.editTitle' : 'chart.form.newTitle')}
      subtitle={t('chart.form.subtitle', { account: accountName })}
      form={form}
      config={config}
      options={{
        lookups: { [CHART_LOOKUPS.type]: types, [CHART_LOOKUPS.group]: groups },
        behaviors: {
          parentId: {
            watch: ['type'],
            state: (values) => ({
              hidden: isMain,
              required: true,
              filter: (item) => groups?.find((g) => g.id === item.id)?.type === values.type,
            }),
          },
        },
      }}
      initialValues={initialValues}
      editing={!!account}
      onSave={save}
    />
  );
}
