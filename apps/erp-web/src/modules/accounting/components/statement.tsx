import type { StatementGroup, StatementLine } from '@erp/api-client';
import { formatMoney } from '@erp/ui';
import { useTranslation } from 'react-i18next';
import type { ReportColumn } from '@/common/reports/ReportPage';

export interface StatementRow {
  key: string;
  accountId?: number;
  code?: string;
  name: string;
  amount?: number;
  strong?: boolean;
}

export const headingRow = (key: string, name: string): StatementRow => ({
  key,
  name,
  strong: true,
});

export const totalRow = (key: string, name: string, amount: number): StatementRow => ({
  key,
  name,
  amount,
  strong: true,
});

export const accountRows = (lines: StatementLine[]): StatementRow[] =>
  lines.map((line) => ({
    key: `account-${line.id}`,
    accountId: line.id,
    code: line.code,
    name: line.name,
    amount: line.amount,
  }));

export function useStatementColumns(): ReportColumn<StatementRow>[] {
  const { t } = useTranslation();
  return [
    { key: 'code', title: t('accounting.code'), value: (row) => row.code, render: codeOf },
    {
      key: 'name',
      title: t('accounting.account'),
      value: (row) => row.name,
      render: (row) => (row.strong ? <strong>{row.name}</strong> : row.name),
    },
    {
      key: 'amount',
      title: t('accounting.amount'),
      type: 'money',
      value: (row) => row.amount,
      render: (row) => {
        if (row.amount == null) return <span />;
        const text = formatMoney(row.amount);
        return row.strong ? <strong>{text}</strong> : text;
      },
    },
  ];
}

const codeOf = (row: StatementRow) => <span>{row.code}</span>;

export function useGroupRows() {
  const { t } = useTranslation();
  return (group: StatementGroup): StatementRow[] => {
    const name = t(`accounting.type.${group.type}`);
    return [
      headingRow(group.type, name),
      ...accountRows(group.lines),
      totalRow(`${group.type}-total`, t('accounting.total', { name }), group.total),
    ];
  };
}
