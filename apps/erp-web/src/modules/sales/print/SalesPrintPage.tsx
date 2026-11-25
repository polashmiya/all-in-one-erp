import { PrinterOutlined } from '@ant-design/icons';
import {
  getGetPrintPartiesQueryKey,
  useGetPrintParties,
  type PrintCustomer,
} from '@erp/api-client';
import { PageContainer, PageState } from '@erp/ui';
import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { useUnitKey } from '@/common/master-data';
import { amountInWords } from './amount-in-words';
import { isPrintable } from './print';
import './print.css';

export interface PrintFact {
  label: string;
  value: ReactNode;
}

export interface PrintColumn<L> {
  key: string;
  title: string;
  end?: boolean;
  render: (line: L) => ReactNode;
}

export interface PrintTotal extends PrintFact {
  strong?: boolean;
}

interface PrintRecord {
  code: string;
  status: string;
  customerId: number;
}

function Facts({ items }: { items: PrintFact[] }) {
  return (
    <dl className="erp-print__facts">
      {items
        .filter((item) => item.value)
        .map((item) => [
          <dt key={`${item.label}-label`}>{item.label}</dt>,
          <dd key={item.label}>{item.value}</dd>,
        ])}
    </dl>
  );
}

export function SalesPrintPage<L extends { id: number }>({
  title,
  backTo,
  loading,
  record,
  notFound,
  partyTitle,
  party,
  facts,
  intro,
  columns,
  lines,
  totals,
  amount,
  notes = [],
  signatures,
}: {
  title: string;
  backTo: string;
  loading: boolean;
  record: PrintRecord | undefined;
  notFound: string;
  partyTitle: string;
  party: (customer: PrintCustomer) => PrintFact[];
  facts: PrintFact[];
  intro?: ReactNode;
  columns: PrintColumn<L>[];
  lines: L[];
  totals: PrintTotal[];
  amount?: number;
  notes?: (string | null | undefined | false)[];
  signatures: string[];
}) {
  const { t } = useTranslation();
  const customerId = record?.customerId ?? 0;
  const parties = useGetPrintParties(customerId, {
    query: {
      queryKey: useUnitKey(getGetPrintPartiesQueryKey(customerId)),
      enabled: customerId > 0,
    },
  });
  const waiting = loading || (customerId > 0 && parties.isPending);
  const printable = !!record && isPrintable(record);
  const shown = printable ? parties.data : undefined;

  return (
    <PageState
      loading={waiting}
      missing={!waiting && !shown}
      missingTitle={record && !printable ? t('print.onlyApproved') : notFound}
      missingActions={[{ key: 'back', label: record?.code ?? t('common.back'), to: backTo }]}
    >
      {record && shown && (
        <PageContainer
          title={title}
          subTitle={record.code}
          crumbs={[record.code]}
          backTo={backTo}
          actions={[
            {
              key: 'print',
              label: t('print.print'),
              icon: <PrinterOutlined />,
              variant: 'primary',
              onClick: () => window.print(),
            },
          ]}
        >
          <article className="erp-print">
            <header className="erp-print__company">
              <h2>{shown.company.name}</h2>
              <p>{shown.company.unitName}</p>
              <p className="erp-print__muted">{shown.company.address}</p>
              <p className="erp-print__muted">
                {[
                  shown.company.phone && `${t('print.phone')}: ${shown.company.phone}`,
                  shown.company.email && `${t('print.email')}: ${shown.company.email}`,
                  shown.company.website,
                  shown.company.taxId && `${t('print.bin')}: ${shown.company.taxId}`,
                ]
                  .filter(Boolean)
                  .join(' · ')}
              </p>
            </header>
            <h3 className="erp-print__title">{title}</h3>
            <section className="erp-print__parties">
              <div>
                <p className="erp-print__muted">{partyTitle}</p>
                <p>
                  <strong>{shown.customer.name}</strong>
                </p>
                <Facts items={party(shown.customer)} />
              </div>
              <Facts items={facts} />
            </section>
            {intro && <p>{intro}</p>}
            {lines.length > 0 && (
              <div className="erp-print__scroll">
                <table className="erp-print__table">
                  <thead>
                    <tr>
                      <th>{t('print.sl')}</th>
                      {columns.map((column) => (
                        <th key={column.key} className={column.end ? 'is-end' : undefined}>
                          {column.title}
                        </th>
                      ))}
                    </tr>
                  </thead>
                  <tbody>
                    {lines.map((line, index) => (
                      <tr key={line.id}>
                        <td>{index + 1}</td>
                        {columns.map((column) => (
                          <td key={column.key} className={column.end ? 'is-end' : undefined}>
                            {column.render(line)}
                          </td>
                        ))}
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
            <dl className="erp-print__totals">
              {totals.map((total) => [
                <dt key={`${total.label}-label`} className={total.strong ? 'is-strong' : undefined}>
                  {total.label}
                </dt>,
                <dd key={total.label} className={total.strong ? 'is-strong' : undefined}>
                  {total.value}
                </dd>,
              ])}
            </dl>
            {amount !== undefined && (
              <p>
                {t('print.inWords')}: <strong>{amountInWords(amount, t)}</strong>
              </p>
            )}
            {notes.filter(Boolean).map((note) => (
              <p key={note || ''}>{note}</p>
            ))}
            <footer className="erp-print__signs">
              {signatures.map((signature) => (
                <span key={signature}>{signature}</span>
              ))}
            </footer>
          </article>
        </PageContainer>
      )}
    </PageState>
  );
}
