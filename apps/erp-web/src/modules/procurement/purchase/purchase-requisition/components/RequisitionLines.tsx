import { DeleteOutlined, PlusOutlined } from '@ant-design/icons';
import { DetailSection, formatMoney, formatNumber, MoneyInput, usePlaceholder } from '@erp/ui';
import { Button, Form, Input, InputNumber, Table, type TableColumnsType } from 'antd';
import type { FormListFieldData } from 'antd/es/form/FormList';
import { useTranslation } from 'react-i18next';
import { LINE_REMARKS_MAX, QUANTITY_DISPLAY_DECIMALS } from '../constants';
import {
  emptyLine,
  lineAmount,
  type RequisitionLineValue,
  type RequisitionValues,
} from '../requisition-form';
import { RequisitionItemSelect } from './RequisitionItemSelect';
import '../requisition.css';

type Line = Partial<RequisitionLineValue>;

export function RequisitionLines() {
  const { t } = useTranslation();
  const placeholder = usePlaceholder();
  const form = Form.useFormInstance<RequisitionValues>();
  const lines: Line[] = Form.useWatch('lines', form) ?? [];
  const totalQuantity = lines.reduce((sum, line) => sum + (line?.quantity ?? 0), 0);
  const totalAmount = lines.reduce((sum, line) => sum + lineAmount(line), 0);

  const columns = (remove: (index: number) => void): TableColumnsType<FormListFieldData> => [
    {
      key: 'item',
      title: t('requisition.lines.item'),
      className: 'erp-lines__item',
      render: (_, field) => {
        const line = lines[field.name];
        return (
          <Form.Item
            name={[field.name, 'itemId']}
            rules={[
              { required: true, message: t('requisition.lines.itemRequired') },
              {
                validator: async (_, value) => {
                  const others = lines.filter((l, i) => i !== field.name && l?.itemId === value);
                  if (value && others.length) throw new Error(t('requisition.lines.duplicate'));
                },
              },
            ]}
          >
            <RequisitionItemSelect
              picked={
                line?.itemId
                  ? { id: line.itemId, code: line.itemCode, name: line.itemName }
                  : undefined
              }
              placeholder={t('requisition.lines.itemPlaceholder')}
              onPick={(option) =>
                form.setFieldValue(['lines', field.name], {
                  ...line,
                  itemId: option.id,
                  itemCode: option.code,
                  itemName: option.name,
                  uomName: option.uomName,
                  rate: option.rate,
                })
              }
            />
          </Form.Item>
        );
      },
    },
    {
      key: 'uom',
      title: t('requisition.lines.uom'),
      render: (_, field) => lines[field.name]?.uomName ?? '—',
    },
    {
      key: 'quantity',
      className: 'erp-lines__quantity',
      title: t('requisition.lines.quantity'),
      align: 'end',
      render: (_, field) => (
        <Form.Item
          name={[field.name, 'quantity']}
          rules={[
            {
              validator: async (_, value) => {
                if (!(Number(value) > 0)) throw new Error(t('requisition.lines.quantityRequired'));
              },
            },
          ]}
        >
          <InputNumber<number>
            min={0}

            controls={false}
            className="erp-w-full"
            placeholder={placeholder('enter', t('requisition.lines.quantity'))}
          />
        </Form.Item>
      ),
    },
    {
      key: 'rate',
      className: 'erp-lines__rate',
      title: t('requisition.lines.rate'),
      align: 'end',
      render: (_, field) => (
        <Form.Item name={[field.name, 'rate']}>
          <MoneyInput
            className="erp-w-full"
            placeholder={placeholder('enter', t('requisition.lines.rate'))}
          />
        </Form.Item>
      ),
    },
    {
      key: 'amount',
      className: 'erp-lines__amount',
      title: t('requisition.lines.amount'),
      align: 'end',
      render: (_, field) => formatMoney(lineAmount(lines[field.name])),
    },
    {
      key: 'remarks',
      className: 'erp-lines__remarks',
      title: t('requisition.lines.remarks'),
      render: (_, field) => (
        <Form.Item name={[field.name, 'remarks']}>
          <Input
            maxLength={LINE_REMARKS_MAX}
            placeholder={t('requisition.lines.remarksPlaceholder')}
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
          aria-label={t('requisition.lines.remove')}
          title={t('requisition.lines.remove')}
          onClick={() => remove(field.name)}
        />
      ),
    },
  ];

  return (
    <DetailSection
      title={t('requisition.lines.title')}
      description={t('requisition.lines.description')}
    >
      <Form.List
        name="lines"
        rules={[
          {
            validator: async (_, value?: Line[]) => {
              if (!value?.length) throw new Error(t('requisition.lines.required'));
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
              summary={() => (
                <Table.Summary.Row>
                  <Table.Summary.Cell index={0} colSpan={2}>
                    <strong>{t('requisition.lines.total')}</strong>
                  </Table.Summary.Cell>
                  <Table.Summary.Cell index={2} align="end">
                    <strong>{formatNumber(totalQuantity, QUANTITY_DISPLAY_DECIMALS)}</strong>
                  </Table.Summary.Cell>
                  <Table.Summary.Cell index={3} />
                  <Table.Summary.Cell index={4} align="end">
                    <strong>{formatMoney(totalAmount)}</strong>
                  </Table.Summary.Cell>
                  <Table.Summary.Cell index={5} colSpan={2} />
                </Table.Summary.Row>
              )}
            />
            <Form.ErrorList errors={errors} />
            <Button type="dashed" icon={<PlusOutlined />} onClick={() => add(emptyLine())}>
              {t('requisition.lines.add')}
            </Button>
          </div>
        )}
      </Form.List>
    </DetailSection>
  );
}
