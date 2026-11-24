import type { ReactNode } from 'react';
import './documents.css';

export interface LineTotal {
  key: string;
  label: ReactNode;
  value: ReactNode;
  strong?: boolean;
}

export function LineTotals({ items }: { items: LineTotal[] }) {
  return (
    <dl className="erp-line-totals">
      {items.map((item) => (
        <div key={item.key} className={item.strong ? 'is-strong' : undefined}>
          <dt>{item.label}</dt>
          <dd>{item.value}</dd>
        </div>
      ))}
    </dl>
  );
}
