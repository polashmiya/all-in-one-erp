import { DeleteOutlined } from '@ant-design/icons';
import {
  DetailSection,
  formatDate,
  formatMoney,
  MoneyInput,
  TitleCell,
  usePlaceholder,
} from '@erp/ui';
import { Button, Form, Table, type TableColumnsType } from 'antd';
import type { FormListFieldData } from 'antd/es/form/FormList';
import { useTranslation } from 'react-i18next';
import '@/common/documents/documents.css';
import { LineTotals } from '@/common/documents/LineTotals';
import type { PayFrom } from '@/common/pay-from';
import { depositTotal, type DepositLine, type DepositValues } from '../deposit-form';

export function DepositLines({ payFrom }: { payFrom: PayFrom }) {
  const { t } = useTranslation();
  const placeholder = usePlaceholder();
  const form = Form.useFormInstance<DepositValues>();
  const lines: DepositLine[] = Form.useWatch('lines', form) ?? [];
  payFrom.useClear();

  const columns = (remove: (index: number) => void): TableColumnsType<FormListFieldData> => [
    {
      key: 'payment',
      title: t('deposit.lines.payment'),
      render: (_, field) => (
        <TitleCell
          title={lines[field.name]?.paymentCode}
          subtitle={formatDate(lines[field.name]?.paymentDate)}
        />
      ),
    },
    {
      key: 'supplier',
      title: t('deposit.lines.supplier'),
      render: (_, field) => lines[field.name]?.supplierName ?? '—',
    },
    {
      key: 'deducted',
      title: t('deposit.lines.deducted'),
      className: 'erp-lines__amount',
      align: 'end',
      render: (_, field) => formatMoney(lines[field.name]?.deducted),
    },
    {
      key: 'remaining',
      title: t('deposit.lines.remaining'),
      className: 'erp-lines__amount',
      align: 'end',
      render: (_, field) => formatMoney(lines[field.name]?.remaining),
    },
    {
      key: 'amount',
      title: t('deposit.lines.amount'),
      className: 'erp-lines__money',
      align: 'end',
      render: (_, field) => (
        <Form.Item
          name={[field.name, 'amount']}
          rules={[
            {
              validator: async (_rule, value) => {
                const remaining = lines[field.name]?.remaining ?? 0;
                if (!(Number(value) > 0)) throw new Error(t('deposit.lines.amountRequired'));
                if (Number(value) > remaining) {
                  throw new Error(
                    t('deposit.lines.overRemaining', { amount: formatMoney(remaining) }),
                  );
                }
              },
            },
          ]}
        >
          <MoneyInput
            compact
            className="erp-w-full"
            placeholder={placeholder('enter', t('deposit.lines.amount'))}
          />
        </Form.Item>
      ),
    },
    {
      key: 'remove',
      render: (_, field) => (
        <Button
          type="text"
          danger
          icon={<DeleteOutlined />}
          aria-label={t('document.lines.remove')}
          title={t('document.lines.remove')}
          onClick={() => remove(field.name)}
        />
      ),
    },
  ];

  return (
    <DetailSection title={t('deposit.lines.title')} description={t('deposit.lines.description')}>
      <Form.List
        name="lines"
        rules={[
          {
            validator: async (_, value?: DepositLine[]) => {
              if (!value?.length) throw new Error(t('deposit.lines.required'));
            },
          },
        ]}
      >
        {(fields, { remove }, { errors }) => (
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
              <span />
              <LineTotals
                items={[
                  {
                    key: 'total',
                    label: t('deposit.total'),
                    value: formatMoney(depositTotal(lines)),
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
