import { DeleteOutlined } from '@ant-design/icons';
import { getGetCollectableBillsQueryKey, useGetCollectableBills } from '@erp/api-client';
import {
  DetailSection,
  Field,
  FieldGrid,
  formatDate,
  formatMoney,
  MoneyInput,
  TitleCell,
  usePlaceholder,
} from '@erp/ui';
import { Button, Form, Input, Table, type TableColumnsType } from 'antd';
import type { FormListFieldData } from 'antd/es/form/FormList';
import { useTranslation } from 'react-i18next';
import { LineTotals } from '@/common/documents/LineTotals';
import { useUnitKey } from '@/common/master-data';
import type { PayFrom } from '@/common/pay-from';
import '@/common/documents/documents.css';
import {
  cents,
  collectionTotals,
  settledOf,
  type CollectionLine,
  type CollectionValues,
} from '../collection-form';
import { AMOUNTS, REMARKS_MAX } from '../constants';

type Amount = (typeof AMOUNTS)[number];

export function CollectionLines({
  collectionId,
  payFrom,
}: {
  collectionId?: number;
  payFrom: PayFrom;
}) {
  const { t } = useTranslation();
  const placeholder = usePlaceholder();
  const form = Form.useFormInstance<CollectionValues>();
  const lines: CollectionLine[] = Form.useWatch('lines', form) ?? [];
  const totals = collectionTotals(lines);
  const isAdvance = !!Form.useWatch('isAdvance', form);
  const customerId = Form.useWatch('customerId', form);
  const params = { customerId: customerId ?? 0, collectionId };
  const receivable = useGetCollectableBills(params, {
    query: { queryKey: useUnitKey(getGetCollectableBillsQueryKey(params)), enabled: !!customerId },
  });
  const advance = receivable.data?.availableAdvance ?? 0;
  payFrom.useClear();

  const input = (key: Amount, disabled = false) => (
    <MoneyInput
      compact
      disabled={disabled}
      className="erp-w-full"
      placeholder={placeholder('enter', t(`collection.lines.${key}`))}
    />
  );
  const detail = (line: number, key: Amount) => (
    <Field name={[line, key]} label={t(`collection.lines.${key}`)}>
      {input(key, key === 'advanceAdjusted' && advance <= 0)}
    </Field>
  );
  const shown = (key: 'billAmount' | 'due') => ({
    key,
    title: t(`collection.lines.${key}`),
    className: 'erp-lines__amount',
    align: 'end' as const,
    render: (_: unknown, field: FormListFieldData) => formatMoney(lines[field.name]?.[key]),
  });

  const columns = (remove: (index: number) => void): TableColumnsType<FormListFieldData> => [
    {
      key: 'bill',
      title: t('collection.lines.bill'),
      className: 'erp-lines__item',
      render: (_, field) => {
        const line = lines[field.name];
        return (
          <TitleCell
            title={line?.code}
            subtitle={[formatDate(line?.deliveryDate), line?.orderCode].filter(Boolean).join(' · ')}
          />
        );
      },
    },
    {
      key: 'dueDate',
      title: t('collection.lines.dueDate'),
      className: 'erp-lines__amount',
      render: (_, field) => {
        const date = lines[field.name]?.dueDate;
        return date ? formatDate(date) : '—';
      },
    },
    shown('billAmount'),
    shown('due'),
    {
      key: 'collectionAmount',
      title: t('collection.lines.collectionAmount'),
      className: 'erp-lines__money',
      align: 'end',
      render: (_, field) => (
        <Form.Item
          name={[field.name, 'collectionAmount']}
          dependencies={AMOUNTS.filter((key) => key !== 'collectionAmount').map((key) => [
            'lines',
            field.name,
            key,
          ])}
          rules={[
            {
              validator: async () => {
                const line: CollectionLine | undefined = form.getFieldValue(['lines', field.name]);
                const settled = settledOf(line);
                if (!(settled > 0)) throw new Error(t('collection.lines.amountRequired'));
                if (cents(settled) > cents(line?.due ?? 0)) {
                  throw new Error(t('collection.lines.overDue', { due: formatMoney(line?.due) }));
                }
              },
            },
          ]}
        >
          {input('collectionAmount')}
        </Form.Item>
      ),
    },
    {
      key: 'settled',
      title: t('collection.lines.settled'),
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
      <DetailSection title={t('collection.lines.title')}>
        <span>{t('collection.lines.advanceDescription')}</span>
      </DetailSection>
    );
  }

  return (
    <DetailSection
      title={t('collection.lines.title')}
      description={t('collection.lines.description')}
    >
      <Form.List
        name="lines"
        rules={[
          {
            validator: async (_, value?: CollectionLine[]) => {
              if (!value?.length) throw new Error(t('collection.lines.required'));
              if (cents(collectionTotals(value).amounts.advanceAdjusted) > cents(advance)) {
                throw new Error(
                  t('collection.lines.overAdvance', { amount: formatMoney(advance) }),
                );
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
              expandable={{
                expandedRowRender: ({ name }) => (
                  <div className="erp-lines__details">
                    <FieldGrid>
                      {detail(name, 'vds')}
                      {detail(name, 'ait')}
                      {detail(name, 'discountAmount')}
                      {detail(name, 'expenseAmount')}
                      {detail(name, 'advanceAdjusted')}
                      <Field name={[name, 'remarks']} label={t('collection.lines.remarks')}>
                        <Input
                          maxLength={REMARKS_MAX}
                          placeholder={placeholder('enter', t('collection.lines.remarks'))}
                        />
                      </Field>
                    </FieldGrid>
                  </div>
                ),
                expandedRowKeys: fields.map((field) => field.key),
                showExpandColumn: false,
              }}
            />
            <Form.ErrorList errors={errors} />
            <div className="erp-lines__footer">
              <span />
              <LineTotals
                items={[
                  ...AMOUNTS.map((key) => ({
                    key,
                    label:
                      key === 'advanceAdjusted'
                        ? t('collection.totals.advance', { amount: formatMoney(advance) })
                        : t(`collection.lines.${key}`),
                    value: formatMoney(totals.amounts[key]),
                  })),
                  {
                    key: 'settled',
                    label: t('collection.lines.settled'),
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
