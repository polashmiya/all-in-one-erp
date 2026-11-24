import type { ChartOfAccountRow } from '@erp/api-client';
import { describe, expect, it } from 'vitest';
import { accountTree, groupKeys, type AccountNode } from './account-tree';

const account = (id: number, code: string, name: string, parentId: number | null = null) =>
  ({ id, code, name, parentId }) as ChartOfAccountRow;

const accounts = [
  account(1, '1', 'Current Assets'),
  account(2, '1100', 'Cash and bank', 1),
  account(3, '1110', 'Cash in hand', 2),
  account(4, '1120', 'Bank', 2),
  account(5, '1300', 'Inventory', 1),
  account(6, '2', 'Non-Current Assets'),
];

const codes = (nodes: AccountNode[]): unknown[] =>
  nodes.map((node) => [node.account.code, ...codes(node.children)]);

describe('accountTree', () => {
  it('puts every account under its parent in order', () => {
    const tree = accountTree(accounts);

    expect(codes(tree)).toEqual([['1', ['1100', ['1110'], ['1120']], ['1300']], ['2']]);
    expect(groupKeys(tree)).toEqual([1, 2]);
  });

  it('keeps the groups above a match', () => {
    expect(codes(accountTree(accounts, ' in hand '))).toEqual([['1', ['1100', ['1110']]]]);
  });

  it('keeps everything under a matching group', () => {
    expect(codes(accountTree(accounts, '1100'))).toEqual([['1', ['1100', ['1110'], ['1120']]]]);
  });
});
