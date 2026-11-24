import { ArrowDownOutlined, ArrowUpOutlined, WalletOutlined } from '@ant-design/icons';
import type { LedgerReport, LedgerRow } from '@erp/api-client';
import { formatMoney, TitleCell, type StatGridItem } from '@erp/ui';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router';
import type { ReportColumn } from '@/common/reports/ReportPage';
import { documentLink } from '@/modules/document-registry';
import { journalPaths } from '../transactions/journal/constants';

export function useBalanceText() {
  const { t } = useTranslation();
  return (amount: number) =>
    amount === 0
      ? formatMoney(0)
      : t(amount > 0 ? 'accounting.dr' : 'accounting.cr', {
          amount: formatMoney(Math.abs(amount)),
        });
}

export function useLedger(data: LedgerReport | undefined, party: ReportColumn<LedgerRow>) {
  const { t } = useTranslation();
  const balance = useBalanceText();

  const columns: ReportColumn<LedgerRow>[] = [
    { key: 'date', title: t('ledger.col.date'), type: 'date', value: (row) => row.date },
    {
      key: 'journal',
      title: t('ledger.col.journal'),
      value: (row) => row.journalCode,
      render: (row) => (
        <TitleCell
          title={<Link to={journalPaths.view(row.journalId)}>{row.journalCode}</Link>}
          subtitle={row.narration}
        />
      ),
    },
    {
      key: 'document',
      title: t('ledger.col.document'),
      value: (row) => row.documentCode,
      render: (row) => {
        const link = documentLink(row.feature);
        return link && row.documentId ? (
          <Link to={link.path(row.documentId)}>{row.documentCode}</Link>
        ) : undefined;
      },
    },
    party,
    {
      key: 'debit',
      title: t('accounting.debit'),
      type: 'money',
      value: (row) => row.debit || null,
    },
    {
      key: 'credit',
      title: t('accounting.credit'),
      type: 'money',
      value: (row) => row.credit || null,
    },
    {
      key: 'balance',
      title: t('accounting.balance'),
      type: 'money',
      value: (row) => row.balance,
      render: (row) => balance(row.balance),
    },
  ];

  const stats: StatGridItem[] = [
    {
      key: 'debit',
      icon: <ArrowDownOutlined />,
      tint: 'green',
      label: t('accounting.debit'),
      value: formatMoney(data?.debit ?? 0),
      caption: `${t('accounting.opening')}: ${balance(data?.opening ?? 0)}`,
    },
    {
      key: 'credit',
      icon: <ArrowUpOutlined />,
      tint: 'amber',
      label: t('accounting.credit'),
      value: formatMoney(data?.credit ?? 0),
    },
    {
      key: 'closing',
      icon: <WalletOutlined />,
      tint: 'blue',
      label: t('accounting.closing'),
      value: balance(data?.closing ?? 0),
    },
  ];

  return { columns, stats };
}
