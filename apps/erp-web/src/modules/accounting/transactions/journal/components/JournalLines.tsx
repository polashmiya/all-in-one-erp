import { DeleteOutlined, PlusOutlined } from '@ant-design/icons';
import { getGetSupplierOptionsQueryKey, useGetSupplierOptions } from '@erp/api-client';
import { selectOptions } from '@erp/metadata-engine';
import { DetailSection, formatMoney, MoneyInput, usePlaceholder } from '@erp/ui';
import { Button, Form, Input, Select, Table, type TableColumnsType } from 'antd';
import type { FormListFieldData } from 'antd/es/form/FormList';
import { useTranslation } from 'react-i18next';
import { LineTotals } from '@/common/documents/LineTotals';
import { useUnitKey } from '@/common/master-data';
import '@/common/documents/documents.css';
import { useAccountOptions } from '../../../accounting';
import { LINE_REMARKS_MAX, MIN_LINES } from '../constants';
import { journalTotals, type JournalLineValue, type JournalValues } from '../journal-form';

const SIDES = ['debit', 'credit'] as const;

export function JournalLines() {
  const { t } = useTranslation();
  const placeholder = usePlaceholder();
  const form = Form.useFormInstance<JournalValues>();
  const lines: JournalLineValue[] = Form.useWatch('lines', form) ?? [];
  const totals = journalTotals(lines);
  const accounts = useAccountOptions();
  const suppliers = useGetSupplierOptions({
    query: { queryKey: useUnitKey(getGetSupplierOptionsQueryKey()) },
  });
  const postable = accounts?.filter((a) => !a.isGroup && !a.postingKeys.includes('inventory'));

  const side = (key: (typeof SIDES)[number]) => ({
    key,
    title: t(`accounting.${key}`),
    className: 'erp-lines__money',
    align: 'end' as const,
    render: (_: unknown, field: FormListFieldData) => {
      const other = key === 'debit' ? 'credit' : 'debit';
      return (
        <Form.Item
          name={[field.name, key]}
          dependencies={[['lines', field.name, other]]}
          rules={[
            {
              validator: async () => {
                const line = form.getFieldValue(['lines', field.name]) as JournalLineValue;
                if (key === 'debit' && !line?.debit === !line?.credit) {
                  throw new Error(t('journal.lines.oneSide'));
                }
              },
            },
          ]}
        >
          <MoneyInput
            compact
            className="erp-w-full"
            placeholder={placeholder('enter', t(`accounting.${key}`))}
          />
        </Form.Item>
      );
    },
  });

  const columns = (remove: (index: number) => void): TableColumnsType<FormListFieldData> => [
    {
      key: 'account',
      title: t('accounting.account'),
      className: 'erp-lines__item',
      render: (_, field) => (
        <div className="erp-lines__cell">
          <Form.Item
            name={[field.name, 'chartOfAccountId']}
            rules={[{ required: true, message: t('journal.lines.accountRequired') }]}
          >
            <Select<number>
              showSearch
              optionFilterProp="label"
              placeholder={placeholder('select', t('accounting.account'))}
              options={selectOptions(postable, lines[field.name]?.chartOfAccountId)}
            />
          </Form.Item>
          <Form.Item name={[field.name, 'remarks']}>
            <Input
              size="small"
              variant="filled"
              maxLength={LINE_REMARKS_MAX}
              placeholder={t('journal.lines.remarks')}
            />
          </Form.Item>
        </div>
      ),
    },
    {
      key: 'supplier',
      title: t('journal.lines.supplier'),
      className: 'erp-lines__item',
      render: (_, field) => (
        <Form.Item
          name={[field.name, 'supplierId']}
          dependencies={[['lines', field.name, 'chartOfAccountId']]}
          rules={[
            {
              validator: async (_rule, value) => {
                const id = form.getFieldValue(['lines', field.name, 'chartOfAccountId']);
                const account = accounts?.find((a) => a.id === id);
                if (account?.postingKeys.includes('accounts-payable') && !value) {
                  throw new Error(t('journal.lines.supplierRequired'));
                }
              },
            },
          ]}
        >
          <Select<number>
            allowClear
            showSearch
            optionFilterProp="label"
            placeholder={placeholder('select', t('journal.lines.supplier'))}
            options={selectOptions(suppliers.data, lines[field.name]?.supplierId)}
          />
        </Form.Item>
      ),
    },
    ...SIDES.map(side),
    {
      key: 'remove',
      render: (_, field) => (
        <Button
          type="text"
          danger
          icon={<DeleteOutlined />}
          aria-label={t('journal.lines.remove')}
          title={t('journal.lines.remove')}
          disabled={lines.length <= MIN_LINES}
          onClick={() => remove(field.name)}
        />
      ),
    },
  ];

  return (
    <DetailSection title={t('journal.lines.title')} description={t('journal.lines.description')}>
      <Form.List
        name="lines"
        rules={[
          {
            validator: async (_, value?: JournalLineValue[]) => {
              if (journalTotals(value ?? []).difference !== 0) {
                throw new Error(t('journal.lines.unbalanced'));
              }
            },
          },
        ]}
      >
        {(fields, { add, remove }, { errors }) => (
          <div className="erp-lines">
            <Table<FormListFieldData>
              size="small"
              rowKey="key"
              pagination={false}
              dataSource={fields}
              columns={columns(remove)}
              scroll={{ x: 'max-content' }}
            />
            <Form.ErrorList errors={errors} />
            <div className="erp-lines__footer">
              <Button type="dashed" icon={<PlusOutlined />} onClick={() => add({})}>
                {t('journal.lines.add')}
              </Button>
              <LineTotals
                items={[
                  {
                    key: 'debit',
                    label: t('journal.totals.debit'),
                    value: formatMoney(totals.debit),
                  },
                  {
                    key: 'credit',
                    label: t('journal.totals.credit'),
                    value: formatMoney(totals.credit),
                  },
                  {
                    key: 'difference',
                    label: t('journal.totals.difference'),
                    value: formatMoney(totals.difference),
                    strong: true,
                  },
                ]}
              />
            </div>
          </div>
        )}
      </Form.List>
    </DetailSection>
  );
}
