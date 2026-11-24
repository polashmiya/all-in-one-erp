import { DeleteOutlined } from '@ant-design/icons';
import {
  DetailSection,
  formatDate,
  formatMoney,
  MoneyInput,
  TitleCell,
  usePlaceholder,
} from '@erp/ui';
import { getGetPayableBillsQueryKey, useGetPayableBills } from '@erp/api-client';
import { Button, Form, Table, type TableColumnsType } from 'antd';
import type { FormListFieldData } from 'antd/es/form/FormList';
import { useTranslation } from 'react-i18next';
import { LineTotals } from '@/common/documents/LineTotals';
import { useUnitKey } from '@/common/master-data';
import type { PayFrom } from '@/common/pay-from';
import {
  cents,
  paymentTotals,
  settledOf,
  type PaymentLine,
  type PaymentValues,
} from '../payment-form';
import '@/common/documents/documents.css';

const AMOUNTS = ['paymentAmount', 'vds', 'tds', 'discountAmount', 'advanceAdjusted'] as const;

export function PaymentLines({ paymentId, payFrom }: { paymentId?: number; payFrom: PayFrom }) {
  const { t } = useTranslation();
  const placeholder = usePlaceholder();
  const form = Form.useFormInstance<PaymentValues>();
  const lines: PaymentLine[] = Form.useWatch('lines', form) ?? [];
  const totals = paymentTotals(lines);
  const isAdvance = !!Form.useWatch('isAdvance', form);
  const supplierId = Form.useWatch('supplierId', form);
  const params = { supplierId: supplierId ?? 0, paymentId };
  const payable = useGetPayableBills(params, {
    query: { queryKey: useUnitKey(getGetPayableBillsQueryKey(params)), enabled: !!supplierId },
  });
  const advance = payable.data?.availableAdvance ?? 0;
  const amounts = AMOUNTS.filter((key) => key !== 'advanceAdjusted' || advance > 0);
  payFrom.useClear();

  const amount = (key: (typeof AMOUNTS)[number]) => ({
    key,
    title: t(`payment.lines.${key}`),
    className: advance > 0 ? 'erp-lines__number' : 'erp-lines__money',
    align: 'end' as const,
    render: (_: unknown, field: FormListFieldData) => (
      <Form.Item
        name={[field.name, key]}
        dependencies={AMOUNTS.filter((other) => other !== key).map((other) => [
          'lines',
          field.name,
          other,
        ])}
        rules={
          key === 'paymentAmount'
            ? [
                {
                  validator: async () => {
                    const line: PaymentLine | undefined = form.getFieldValue(['lines', field.name]);
                    const settled = settledOf(line);
                    if (!(settled > 0)) throw new Error(t('payment.lines.amountRequired'));
                    if (cents(settled) > cents(line?.due ?? 0)) {
                      throw new Error(t('payment.lines.overDue', { due: formatMoney(line?.due) }));
                    }
                  },
                },
              ]
            : undefined
        }
      >
        <MoneyInput
          compact
          className="erp-w-full"
          placeholder={placeholder('enter', t(`payment.lines.${key}`))}
        />
      </Form.Item>
    ),
  });

  const columns = (remove: (index: number) => void): TableColumnsType<FormListFieldData> => [
    {
      key: 'bill',
      title: t('payment.lines.bill'),
      render: (_, field) => {
        const line = lines[field.name];
        return (
          <TitleCell
            title={line?.code}
            subtitle={[formatDate(line?.receiveDate), line?.purchaseOrderName]
              .filter(Boolean)
              .join(' · ')}
          />
        );
      },
    },
    {
      key: 'billAmount',
      title: t('payment.lines.billAmount'),
      className: 'erp-lines__amount',
      align: 'end',
      render: (_, field) => formatMoney(lines[field.name]?.billAmount),
    },
    {
      key: 'due',
      title: t('payment.lines.due'),
      className: 'erp-lines__amount',
      align: 'end',
      render: (_, field) => formatMoney(lines[field.name]?.due),
    },
    ...amounts.map(amount),
    {
      key: 'settled',
      title: t('payment.lines.settled'),
      className: 'erp-lines__amount',
      align: 'end',
      render: (_, field) => formatMoney(settledOf(lines[field.name])),
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

  if (isAdvance) {
    return (
      <DetailSection title={t('payment.lines.title')}>
        <span>{t('payment.lines.advanceDescription')}</span>
      </DetailSection>
    );
  }

  return (
    <DetailSection title={t('payment.lines.title')} description={t('payment.lines.description')}>
      <Form.List
        name="lines"
        rules={[
          {
            validator: async (_, value?: PaymentLine[]) => {
              if (!value?.length) throw new Error(t('payment.lines.required'));
              if (cents(paymentTotals(value).advance) > cents(advance)) {
                throw new Error(t('payment.lines.overAdvance', { amount: formatMoney(advance) }));
              }
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
                    key: 'payment',
                    label: t('payment.totals.payment'),
                    value: formatMoney(totals.payment),
                  },
                  { key: 'vds', label: t('payment.totals.vds'), value: formatMoney(totals.vds) },
                  { key: 'tds', label: t('payment.totals.tds'), value: formatMoney(totals.tds) },
                  {
                    key: 'discount',
                    label: t('payment.totals.discount'),
                    value: formatMoney(totals.discount),
                  },
                  ...(advance > 0
                    ? [
                        {
                          key: 'advance',
                          label: t('payment.totals.advance', { amount: formatMoney(advance) }),
                          value: formatMoney(totals.advance),
                        },
                      ]
                    : []),
                  {
                    key: 'settled',
                    label: t('payment.totals.settled'),
                    value: formatMoney(totals.settled),
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
