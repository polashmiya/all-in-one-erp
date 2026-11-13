import { ApartmentOutlined, ShopOutlined, UndoOutlined } from '@ant-design/icons';
import { ActionGroup } from '@erp/ui';
import { Alert } from 'antd';
import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';

interface UnitOption {
  value: string;
  label: string;
}

function ScopeOption({
  active,
  icon,
  name,
  status,
  own,
  onClick,
}: {
  active: boolean;
  icon: ReactNode;
  name: string;
  status: string;
  own?: boolean;
  onClick: () => void;
}) {
  return (
    <button
      type="button"
      role="radio"
      aria-checked={active}
      className={`acc-scope__option${active ? ' is-active' : ''}`}
      onClick={onClick}
    >
      <span className="acc-scope__icon">{icon}</span>
      <span className="acc-scope__text">
        <span className="acc-scope__name">{name}</span>
        <span className={`acc-scope__status${own ? ' is-own' : ''}`}>{status}</span>
      </span>
    </button>
  );
}

/**
 * Chooses whose layout is being arranged — the account's default layout or one business unit's — and says
 * plainly who a change reaches.
 */
export function LayoutScope({
  units,
  unitId,
  ownLayout,
  layoutUnitIds,
  resetting,
  onChoose,
  onReset,
}: {
  units: UnitOption[];
  unitId?: string;
  /** The chosen scope stores its own layout: a changed default layout, or a business unit's own layout. */
  ownLayout: boolean;
  layoutUnitIds: string[];
  resetting: boolean;
  onChoose: (unitId: string | null) => void;
  onReset: () => unknown;
}) {
  const { t } = useTranslation();
  const unit = units.find((u) => u.value === unitId)?.label ?? '';
  const following = units.filter((u) => !layoutUnitIds.includes(u.value));

  const banner = !unitId
    ? {
        type: 'info' as const,
        title: t('layout.banner.default'),
        text: following.length
          ? t('layout.banner.defaultUsed', { units: following.map((u) => u.label).join(', ') })
          : t('layout.banner.defaultUnused'),
        reset: ownLayout && {
          label: t('layout.resetDefault'),
          title: t('layout.resetDefaultConfirm'),
          description: t('layout.resetDefaultHint'),
        },
      }
    : ownLayout
      ? {
          type: 'success' as const,
          title: t('layout.banner.own', { unit }),
          text: t('layout.banner.ownHint', { unit }),
          reset: {
            label: t('layout.resetUnit'),
            title: t('layout.resetUnitConfirm', { unit }),
            description: t('layout.resetUnitHint', { unit }),
          },
        }
      : {
          type: 'warning' as const,
          title: t('layout.banner.follows', { unit }),
          text: t('layout.banner.followsHint', { unit }),
          reset: false as const,
        };

  return (
    <section className="acc-scope">
      <span className="acc-scope__label" id="acc-scope-label">
        {t('layout.scope.label')}
      </span>
      <div className="acc-scope__options" role="radiogroup" aria-labelledby="acc-scope-label">
        <ScopeOption
          active={!unitId}
          icon={<ApartmentOutlined />}
          name={t('layout.scope.default')}
          status={
            following.length
              ? t('layout.scope.usedBy', { count: following.length })
              : t('layout.scope.usedByNone')
          }
          onClick={() => onChoose(null)}
        />
        {units.map((option) => {
          const own = layoutUnitIds.includes(option.value);
          return (
            <ScopeOption
              key={option.value}
              active={option.value === unitId}
              icon={<ShopOutlined />}
              name={option.label}
              status={t(own ? 'layout.scope.own' : 'layout.scope.follows')}
              own={own}
              onClick={() => onChoose(option.value)}
            />
          );
        })}
      </div>
      <Alert
        type={banner.type}
        showIcon
        message={banner.title}
        description={banner.text}
        action={
          banner.reset && (
            <ActionGroup
              size="small"
              actions={[
                {
                  key: 'reset',
                  label: banner.reset.label,
                  icon: <UndoOutlined />,
                  loading: resetting,
                  confirm: {
                    title: banner.reset.title,
                    description: banner.reset.description,
                    okText: banner.reset.label,
                    danger: true,
                  },
                  onClick: onReset,
                },
              ]}
            />
          )
        }
      />
    </section>
  );
}
