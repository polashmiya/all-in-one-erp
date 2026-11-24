import type { ChartOfAccountRow } from '@erp/api-client';

export interface AccountNode {
  key: number;
  account: ChartOfAccountRow;
  children: AccountNode[];
}

const matches = ({ code, name }: ChartOfAccountRow, term: string) =>
  `${code} ${name}`.toLowerCase().includes(term);

export function accountTree(accounts: ChartOfAccountRow[], search = ''): AccountNode[] {
  const term = search.trim().toLowerCase();

  const under = (parentId: number | null, shown: boolean): AccountNode[] =>
    accounts
      .filter((account) => account.parentId === parentId)
      .flatMap((account) => {
        const found = shown || matches(account, term);
        const children = under(account.id, found);
        return found || children.length ? [{ key: account.id, account, children }] : [];
      });

  return under(null, term === '');
}

export const groupKeys = (nodes: AccountNode[]): number[] =>
  nodes.flatMap((node) => (node.children.length ? [node.key, ...groupKeys(node.children)] : []));
