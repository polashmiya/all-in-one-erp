import { ApartmentOutlined, InboxOutlined, PlusOutlined, ShopOutlined } from '@ant-design/icons';
import { Button, Col, Empty, Flex, Form, Input, Row, Select, Space, Typography } from 'antd';
import { useTranslation } from 'react-i18next';
import { usePlaceholder } from '@erp/ui';
import { setupRules, suggestCodeOnBlur } from '../../model/rules';
import { normalizeCode, UNIT_LEVEL, type BusinessUnitForm } from '../../model/setup-model';
import { ItemCard } from './ItemCard';
import { UnitTabs } from './UnitTabs';

function WarehouseList({ unitIndex, unit }: { unitIndex: number; unit: BusinessUnitForm }) {
  const { t } = useTranslation();
  const placeholder = usePlaceholder();
  const form = Form.useFormInstance();
  const rules = setupRules(t);
  const listPath = ['businessUnits', unitIndex, 'warehouses'];
  const offices = (unit.offices ?? []).filter((o) => o?.code);
  const unitName = unit.name?.trim() || t('setup.unitN', { n: unitIndex + 1 });

  const locationOptions = [
    {
      value: UNIT_LEVEL,
      label: (
        <Space size={6}>
          <ApartmentOutlined />
          {t('setup.unitLevel', { unit: unitName })}
        </Space>
      ),
    },
    ...offices.map((office) => ({
      value: office.code!,
      label: (
        <Space size={6}>
          <ShopOutlined />
          {office.code} · {office.name || t('setup.untitled')}
        </Space>
      ),
    })),
  ];

  return (
    <Form.List name={listPath}>
      {(fields, { add, remove }) => (
        <Flex vertical gap={12}>
          {fields.length === 0 && <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description={t('setup.noWarehouses')} />}
          {fields.map((field, index) => {
            const path = [...listPath, field.name];
            return (
              <ItemCard
                key={field.key}
                path={path}
                icon={<InboxOutlined />}
                fallbackTitle={t('setup.warehouseN', { n: index + 1 })}
                onRemove={() => remove(field.name)}
              >
                <Row gutter={16}>
                  <Col xs={24} md={8}>
                    <Form.Item name={[field.name, 'code']} label={t('field.unitCode')} normalize={normalizeCode} rules={[...rules.code, rules.uniqueIn(listPath, field.name)]}>
                      <Input placeholder="CWH" className="erp-code" maxLength={32} />
                    </Form.Item>
                  </Col>
                  <Col xs={24} md={16}>
                    <Form.Item name={[field.name, 'name']} label={t('field.unitName')} rules={[rules.required, { max: 200 }]}>
                      <Input placeholder={t('setup.placeholder.warehouseName')} onBlur={suggestCodeOnBlur(form, path)} />
                    </Form.Item>
                  </Col>
                  <Col xs={24} md={12}>
                    <Form.Item
                      name={[field.name, 'officeCode']}
                      label={t('field.location')}
                      tooltip={t('setup.locationHelp')}
                      rules={[
                        {
                          validator: async (_, value?: string) => {
                            if (value && !offices.some((o) => o.code === value)) throw new Error(t('setup.officeMissing'));
                          },
                        },
                      ]}
                    >
                      <Select options={locationOptions} placeholder={placeholder('select', t('field.location'))} />
                    </Form.Item>
                  </Col>
                  <Col xs={24} md={12}>
                    <Form.Item name={[field.name, 'address']} label={t('field.address')} rules={[{ max: 500 }]} className="erp-flush">
                      <Input placeholder={placeholder('enter', t('field.address'))} />
                    </Form.Item>
                  </Col>
                </Row>
              </ItemCard>
            );
          })}
          <Button type="dashed" block icon={<PlusOutlined />} onClick={() => add({ officeCode: UNIT_LEVEL })}>
            {t('setup.addWarehouse')}
          </Button>
        </Flex>
      )}
    </Form.List>
  );
}

export function WarehousesStep({ activeUnit, onUnitChange }: { activeUnit: number; onUnitChange: (index: number) => void }) {
  const { t } = useTranslation();

  return (
    <>
      <Typography.Paragraph type="secondary">{t('setup.warehousesIntro')}</Typography.Paragraph>
      <UnitTabs
        activeUnit={activeUnit}
        onChange={onUnitChange}
        count={(unit) => unit.warehouses?.length ?? 0}
        renderUnit={(unitIndex, unit) => <WarehouseList unitIndex={unitIndex} unit={unit} />}
      />
    </>
  );
}
