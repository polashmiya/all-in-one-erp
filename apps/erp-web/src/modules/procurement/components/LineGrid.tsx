import { DeleteOutlined, PlusOutlined } from '@ant-design/icons';
import type { ProcurementItemOption } from '@erp/api-client';
import { DetailSection, usePlaceholder } from '@erp/ui';
import { Button, Form, InputNumber, Table, type TableColumnsType } from 'antd';
import type { FormListFieldData } from 'antd/es/form/FormList';
import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { ItemSelect } from './ItemSelect';
import { LineTotals, type LineTotal } from './LineTotals';
import '../procurement.css';

export interface BaseLine {
  itemId?: number | null;
  itemCode?: string;
  itemName?: string;
  uomName?: string;
  quantity?: number | null;
}

export interface LineColumn<L> {
  key: string;
  title: string;
  className?: string;
  align?: 'end';
  render: (field: FormListFieldData, line: L | undefined) => ReactNode;
}

export function LineGrid<L extends BaseLine>({
  description,
  columns,
  newLine,
  onPick,
  totals,
}: {
  description: string;
  columns: LineColumn<L>[];
  newLine: () => Partial<L>;
  onPick: (option: ProcurementItemOption) => Partial<L>;
  totals: (lines: L[]) => LineTotal[];
}) {
  const { t } = useTranslation();
  const placeholder = usePlaceholder();
  const form = Form.useFormInstance();
  const lines: L[] = Form.useWatch('lines', form) ?? [];

  const tableColumns = (remove: (index: number) => void): TableColumnsType<FormListFieldData> => [
    {
      key: 'item',
      title: t('procurement.lines.item'),
      className: 'erp-lines__item',
      render: (_, field) => {
        const line = lines[field.name];
        return (
          <Form.Item
            name={[field.name, 'itemId']}
            rules={[
              { required: true, message: t('procurement.lines.itemRequired') },
              {
                validator: async (_, value) => {
                  const others = lines.filter((l, i) => i !== field.name && l?.itemId === value);
                  if (value && others.length) throw new Error(t('procurement.lines.duplicate'));
                },
              },
            ]}
          >
            <ItemSelect
              picked={
                line?.itemId
                  ? { id: line.itemId, code: line.itemCode, name: line.itemName }
                  : undefined
              }
              placeholder={t('procurement.lines.itemPlaceholder')}
              onPick={(option) =>
                form.setFieldValue(['lines', field.name], {
                  ...line,
                  itemId: option.id,
                  itemCode: option.code,
                  itemName: option.name,
                  uomName: option.uomName,
                  ...onPick(option),
                })
              }
            />
          </Form.Item>
        );
      },
    },
    {
      key: 'uom',
      title: t('procurement.lines.uom'),
      render: (_, field) => lines[field.name]?.uomName ?? '—',
    },
    {
      key: 'quantity',
      className: 'erp-lines__number',
      title: t('procurement.lines.quantity'),
      align: 'end',
      render: (_, field) => (
        <Form.Item
          name={[field.name, 'quantity']}
          rules={[
            {
              validator: async (_, value) => {
                if (!(Number(value) > 0)) throw new Error(t('procurement.lines.quantityRequired'));
              },
            },
          ]}
        >
          <InputNumber<number>
            min={0}
            controls={false}
            className="erp-w-full"
            placeholder={placeholder('enter', t('procurement.lines.quantity'))}
          />
        </Form.Item>
      ),
    },
    ...columns.map((column) => ({
      key: column.key,
      title: column.title,
      className: column.className,
      align: column.align,
      render: (_: unknown, field: FormListFieldData) => column.render(field, lines[field.name]),
    })),
    {
      key: 'remove',
      render: (_, field) => (
        <Button
          type="text"
          danger
          icon={<DeleteOutlined />}
          aria-label={t('procurement.lines.remove')}
          title={t('procurement.lines.remove')}
          onClick={() => remove(field.name)}
        />
      ),
    },
  ];

  return (
    <DetailSection title={t('procurement.lines.title')} description={description}>
      <Form.List
        name="lines"
        rules={[
          {
            validator: async (_, value?: L[]) => {
              if (!value?.length) throw new Error(t('procurement.lines.required'));
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
              columns={tableColumns(remove)}
              scroll={{ x: 'max-content' }}
            />
            <Form.ErrorList errors={errors} />
            <div className="erp-lines__footer">
              <Button type="dashed" icon={<PlusOutlined />} onClick={() => add(newLine())}>
                {t('procurement.lines.add')}
              </Button>
              <LineTotals items={totals(lines)} />
            </div>
          </div>
        )}
      </Form.List>
    </DetailSection>
  );
}
