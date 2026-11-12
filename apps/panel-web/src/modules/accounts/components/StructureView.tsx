import {
  ApartmentOutlined,
  EditOutlined,
  InboxOutlined,
  PlusOutlined,
  ShopOutlined,
} from '@ant-design/icons';
import { ActionGroup, EmptyState, StatusTag, type ActionItem } from '@erp/ui';
import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import {
  STRUCTURE_TINT,
  type StructureKind,
  type StructureLeaf,
  type StructureOffice,
  type StructureUnit,
} from './structure';

export interface StructureTarget {
  kind: StructureKind;
  node?: StructureLeaf;
  unitKey?: string;
  officeKey?: string;
}

interface Props {
  units: StructureUnit[];
  compact?: boolean;
  empty?: ReactNode;
  onAdd?: (target: StructureTarget) => void;
  onEdit?: (target: StructureTarget) => void;
}

function Name({ node, tint, icon }: { node: StructureLeaf; tint: string; icon: ReactNode }) {
  const { t } = useTranslation();
  return (
    <span className="acc-org__name">
      <span className={`erp-tile erp-tile--${tint} is-sm`} aria-hidden="true">
        {icon}
      </span>
      <span className="acc-org__text">
        <span className={node.name ? 'acc-org__title' : 'acc-org__title is-muted'}>
          {node.name || t('setup.untitled')}
        </span>
        {node.code && <span className="erp-code acc-org__code">{node.code}</span>}
      </span>
      {!node.isActive && <StatusTag>{t('accounts.status.inactive')}</StatusTag>}
    </span>
  );
}

function Warehouses({
  items,
  unitKey,
  officeKey,
  onEdit,
}: {
  items: StructureLeaf[];
  unitKey: string;
  officeKey?: string;
  onEdit?: Props['onEdit'];
}) {
  const { t } = useTranslation();
  if (items.length === 0) return <p className="acc-org__empty">{t('structure.noWarehouses')}</p>;

  return (
    <ul className="acc-org__list">
      {items.map((w) => (
        <li key={w.key} className={w.isActive ? 'acc-org__row' : 'acc-org__row is-inactive'}>
          <InboxOutlined className={`erp-fg--${STRUCTURE_TINT.warehouse}`} />
          <span className="acc-org__row-name">{w.name || t('setup.untitled')}</span>
          {w.code && <span className="erp-code acc-org__code">{w.code}</span>}
          {!w.isActive && <StatusTag>{t('accounts.status.inactive')}</StatusTag>}
          {onEdit && (
            <ActionGroup
              size="small"
              className="acc-org__row-action"
              actions={[
                {
                  key: 'edit',
                  label: t('structure.edit.warehouse'),
                  icon: <EditOutlined />,
                  variant: 'text',
                  iconOnly: true,
                  onClick: () => onEdit({ kind: 'warehouse', node: w, unitKey, officeKey }),
                },
              ]}
            />
          )}
        </li>
      ))}
    </ul>
  );
}

function OfficeCard({
  office,
  unitKey,
  onAdd,
  onEdit,
}: {
  office: StructureOffice;
  unitKey: string;
} & Pick<Props, 'onAdd' | 'onEdit'>) {
  const { t } = useTranslation();
  const actions: ActionItem[] = [
    {
      key: 'add',
      label: t('structure.add.warehouse'),
      icon: <PlusOutlined />,
      variant: 'text',
      iconOnly: true,
      hidden: !onAdd,
      onClick: () => onAdd?.({ kind: 'warehouse', unitKey, officeKey: office.key }),
    },
    {
      key: 'edit',
      label: t('structure.edit.office'),
      icon: <EditOutlined />,
      variant: 'text',
      iconOnly: true,
      hidden: !onEdit,
      onClick: () => onEdit?.({ kind: 'office', node: office, unitKey }),
    },
  ];

  return (
    <div className={office.isActive ? 'acc-org__office' : 'acc-org__office is-inactive'}>
      <div className="acc-org__head">
        <Name node={office} tint={STRUCTURE_TINT.office} icon={<ShopOutlined />} />
        <ActionGroup size="small" actions={actions} />
      </div>
      <Warehouses
        items={office.warehouses}
        unitKey={unitKey}
        officeKey={office.key}
        onEdit={onEdit}
      />
    </div>
  );
}

function UnitCard({
  unit,
  compact,
  onAdd,
  onEdit,
}: { unit: StructureUnit } & Omit<Props, 'units'>) {
  const { t } = useTranslation();
  const warehouses =
    unit.warehouses.length + unit.offices.reduce((s, o) => s + o.warehouses.length, 0);

  return (
    <section className={unit.isActive ? 'acc-org__unit' : 'acc-org__unit is-inactive'}>
      <header className="acc-org__unit-head">
        <Name node={unit} tint={STRUCTURE_TINT.unit} icon={<ApartmentOutlined />} />
        <span className="acc-org__counts">
          {t('structure.offices', { count: unit.offices.length })} ·{' '}
          {t('structure.warehouses', { count: warehouses })}
        </span>
        <ActionGroup
          size="small"
          className="acc-org__unit-actions"
          actions={[
            {
              key: 'office',
              label: t('structure.add.office'),
              icon: <ShopOutlined />,
              hidden: !onAdd,
              onClick: () => onAdd?.({ kind: 'office', unitKey: unit.key }),
            },
            {
              key: 'warehouse',
              label: t('structure.add.warehouse'),
              icon: <InboxOutlined />,
              hidden: !onAdd,
              onClick: () => onAdd?.({ kind: 'warehouse', unitKey: unit.key }),
            },
            {
              key: 'edit',
              label: t('structure.edit.unit'),
              icon: <EditOutlined />,
              iconOnly: true,
              hidden: !onEdit,
              onClick: () => onEdit?.({ kind: 'business-unit', node: unit }),
            },
          ]}
        />
      </header>

      <div className={compact ? 'acc-org__grid is-compact' : 'acc-org__grid'}>
        {unit.offices.map((office) => (
          <OfficeCard
            key={office.key}
            office={office}
            unitKey={unit.key}
            onAdd={onAdd}
            onEdit={onEdit}
          />
        ))}
        {(unit.warehouses.length > 0 || unit.offices.length === 0) && (
          <div className="acc-org__office is-direct">
            <div className="acc-org__head">
              <span className="acc-org__direct">{t('structure.direct')}</span>
            </div>
            <Warehouses items={unit.warehouses} unitKey={unit.key} onEdit={onEdit} />
          </div>
        )}
      </div>
    </section>
  );
}

export function StructureView({ units, compact, empty, onAdd, onEdit }: Props) {
  if (units.length === 0) return <>{empty ?? <EmptyState title={<StructureEmpty />} />}</>;

  return (
    <div className="acc-org">
      {units.map((unit) => (
        <UnitCard key={unit.key} unit={unit} compact={compact} onAdd={onAdd} onEdit={onEdit} />
      ))}
    </div>
  );
}

function StructureEmpty() {
  const { t } = useTranslation();
  return <>{t('structure.empty')}</>;
}
