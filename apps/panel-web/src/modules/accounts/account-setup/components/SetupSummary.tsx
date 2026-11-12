import {
  ApartmentOutlined,
  AppstoreOutlined,
  BankOutlined,
  InboxOutlined,
  ShopOutlined,
} from '@ant-design/icons';
import { InitialsTile, SectionCard, StatusTag } from '@erp/ui';
import { Typography } from 'antd';
import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { STRUCTURE_TINT, structureFromForm } from '../../components/structure';
import { StructureView } from '../../components/StructureView';
import { countStructure, type SetupFormValues } from '../model/setup-model';

function Count({ icon, value, label }: { icon: ReactNode; value: number; label: string }) {
  return (
    <div className="acc-count">
      <span className="acc-count__icon">{icon}</span>
      <span className="acc-count__value">{value}</span>
      <span className="acc-count__label">{label}</span>
    </div>
  );
}

export function SetupSummary({ values }: { values: SetupFormValues | undefined }) {
  const { t } = useTranslation();
  const name = values?.account?.name?.trim();
  const code = values?.account?.code;
  const counts = countStructure(values?.businessUnits);

  return (
    <SectionCard title={t('setup.summary')} padding="lg">
      <div className="acc-summary__head">
        {name ? (
          <InitialsTile name={name} />
        ) : (
          <span className="erp-tile erp-tile--primary">
            <BankOutlined />
          </span>
        )}
        <div className="erp-stack is-tight">
          <Typography.Text strong ellipsis type={name ? undefined : 'secondary'}>
            {name || t('setup.unnamed')}
          </Typography.Text>
          {code && <StatusTag>{code}</StatusTag>}
        </div>
      </div>

      <div className="acc-summary__counts">
        <Count
          icon={<ApartmentOutlined className={`erp-fg--${STRUCTURE_TINT.unit}`} />}
          value={counts.businessUnits}
          label={t('accounts.stat.businessUnits')}
        />
        <Count
          icon={<ShopOutlined className={`erp-fg--${STRUCTURE_TINT.office}`} />}
          value={counts.offices}
          label={t('accounts.stat.offices')}
        />
        <Count
          icon={<InboxOutlined className={`erp-fg--${STRUCTURE_TINT.warehouse}`} />}
          value={counts.warehouses}
          label={t('accounts.stat.warehouses')}
        />
      </div>

      {values?.featureCodes !== undefined && (
        <span className="acc-summary__note">
          <AppstoreOutlined />
          {t('setup.menusSelected', { count: values.featureCodes.length })}
        </span>
      )}

      <div className="acc-divider" />
      <StructureView units={structureFromForm(values?.businessUnits)} compact />
    </SectionCard>
  );
}
