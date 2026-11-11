import type { ReactNode } from 'react';
import { ActionGroup } from '../actions/ActionGroup';
import { BackButton } from '../actions/BackButton';
import type { ActionItem } from '../actions/types';
import type { Tint } from '../../theme/design-tokens';

export interface RecordMeta {
  key: string;
  icon?: ReactNode;
  label: ReactNode;
}

export interface RecordHeaderProps {
  icon?: ReactNode;
  tint?: Tint;
  title: ReactNode;
  tags?: ReactNode;
  meta?: RecordMeta[];
  actions?: ActionItem[];
  backTo?: string;
}

export function RecordHeader({
  icon,
  tint = 'primary',
  title,
  tags,
  meta = [],
  actions = [],
  backTo,
}: RecordHeaderProps) {
  return (
    <section className="erp-card erp-record">
      {backTo && (
        <div className="erp-record__back">
          <BackButton to={backTo} />
        </div>
      )}
      <div className="erp-record__hero">
        {icon && (
          <span className={`erp-tile erp-tile--${tint} erp-record__image`} aria-hidden="true">
            {icon}
          </span>
        )}
        <h1 className="erp-record__title">{title}</h1>
        {tags && <div className="erp-record__tags">{tags}</div>}
      </div>
      {meta.length > 0 && (
        <ul className="erp-record__meta">
          {meta.map((item) => (
            <li key={item.key}>
              {item.icon}
              <span>{item.label}</span>
            </li>
          ))}
        </ul>
      )}
      {actions.length > 0 && <ActionGroup actions={actions} className="erp-record__actions" />}
    </section>
  );
}
