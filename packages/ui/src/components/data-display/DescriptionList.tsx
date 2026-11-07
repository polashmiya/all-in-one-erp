import type { ReactNode } from 'react';

export interface DescriptionItem {
  key: string;
  label: ReactNode;
  value: ReactNode;
  labelExtra?: ReactNode;
  code?: boolean;
  wide?: boolean;
}

export function DescriptionList({
  items,
  minColumnWidth = 200,
  columns,
}: {
  items: DescriptionItem[];
  minColumnWidth?: number;
  columns?: 2 | 3 | 4;
}) {
  return (
    <dl
      className={columns ? `erp-dl erp-dl--grid erp-field-grid--${columns}` : 'erp-dl'}
      style={
        columns
          ? undefined
          : { gridTemplateColumns: `repeat(auto-fit, minmax(${minColumnWidth}px, 1fr))` }
      }
    >
      {items.map((item) => {
        const empty = item.value === null || item.value === undefined || item.value === '';
        return (
          <div key={item.key} className={item.wide ? 'erp-dl__item is-wide' : 'erp-dl__item'}>
            <dt>
              {item.label}
              {item.labelExtra}
            </dt>
            <dd className={empty ? 'is-empty' : item.code ? 'is-code' : undefined}>
              {empty ? '—' : item.value}
            </dd>
          </div>
        );
      })}
    </dl>
  );
}

export function DetailSection({
  title,
  description,
  extra,
  children,
}: {
  title: ReactNode;
  description?: ReactNode;
  extra?: ReactNode;
  children: ReactNode;
}) {
  return (
    <section className="erp-detail-section">
      <div className="erp-detail-section__head">
        <div>
          <h3>{title}</h3>
          {description && <p>{description}</p>}
        </div>
        {extra}
      </div>
      {children}
    </section>
  );
}
