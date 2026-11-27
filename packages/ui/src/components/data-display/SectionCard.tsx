import type { ReactNode } from 'react';
import { ActionGroup } from '../actions/ActionGroup';
import type { ActionItem } from '../actions/types';

export type CardPadding = 'none' | 'md' | 'lg';

export interface SectionCardProps {
  title?: ReactNode;
  subtitle?: ReactNode;
  extra?: ReactNode;
  actions?: ActionItem[];
  padding?: CardPadding;
  fill?: boolean;
  className?: string;
  children?: ReactNode;
}

export function SectionCard({
  title,
  subtitle,
  extra,
  actions,
  padding = 'md',
  fill = false,
  className,
  children,
}: SectionCardProps) {
  const hasExtra = !!extra || !!actions?.length;
  return (
    <section className={['erp-card', fill && 'is-fill', className].filter(Boolean).join(' ')}>
      {(title || hasExtra) && (
        <header className="erp-card__head">
          <div className="erp-card__titles">
            {title && <h2 className="erp-card__title">{title}</h2>}
            {subtitle && <span className="erp-card__sub">{subtitle}</span>}
          </div>
          {hasExtra && (
            <div className="erp-card__extra">
              {extra}
              {actions && <ActionGroup actions={actions} size="small" />}
            </div>
          )}
        </header>
      )}
      <div className={`erp-card__body erp-card__body--${padding}`}>{children}</div>
    </section>
  );
}
