import { EditOutlined, PlusOutlined } from '@ant-design/icons';
import type { ChartOfAccountRow } from '@erp/api-client';
import { EmptyState, FilterBar, RowActions, StatusTag } from '@erp/ui';
import { Skeleton, Tree } from 'antd';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { accountTree, groupKeys, type AccountNode } from '../account-tree';
import { CHART_FEATURE } from '../constants';
import './account-tree.css';

export function AccountTree({
  accounts,
  search,
  onAdd,
  onEdit,
}: {
  accounts?: ChartOfAccountRow[];
  search: string;
  onAdd: (parent: ChartOfAccountRow) => void;
  onEdit: (account: ChartOfAccountRow) => void;
}) {
  const { t } = useTranslation();
  const access = useFeatureAccess(CHART_FEATURE);
  const [expanded, setExpanded] = useState<number[]>([]);

  const searching = search.trim() !== '';
  const nodes = accountTree(accounts ?? [], search);
  const groups = groupKeys(nodes);

  const add = (parent: ChartOfAccountRow) => {
    setExpanded((keys) => [...new Set([...keys, parent.id])]);
    onAdd(parent);
  };

  const row = ({ account }: AccountNode) => (
    <div className="erp-account-tree__row">
      <span className={account.parentId ? undefined : 'erp-account-tree__main'}>
        {account.code} - {account.name}
      </span>
      {!account.isActive && <StatusTag tone="neutral">{t('master.inactive')}</StatusTag>}
      <span className="erp-account-tree__actions" onClick={(event) => event.stopPropagation()}>
        <RowActions
          actions={[
            {
              key: 'add',
              label: t('chart.addUnder', { name: account.name }),
              icon: <PlusOutlined />,
              hidden: !access.create || !account.isGroup,
              onClick: () => add(account),
            },
            {
              key: 'edit',
              label: t('master.edit'),
              icon: <EditOutlined />,
              hidden: !access.edit,
              onClick: () => onEdit(account),
            },
          ]}
        />
      </span>
    </div>
  );

  return (
    <section className="erp-card erp-account-tree">
      <FilterBar>
        <button
          type="button"
          className="erp-linkbtn"
          onClick={() => setExpanded(groups)}
          disabled={searching}
        >
          {t('chart.expandAll')}
        </button>
        <button
          type="button"
          className="erp-linkbtn"
          onClick={() => setExpanded([])}
          disabled={searching}
        >
          {t('chart.collapseAll')}
        </button>
      </FilterBar>
      <div className="erp-account-tree__body">
        {!accounts ? (
          <Skeleton active />
        ) : nodes.length ? (
          <Tree<AccountNode>
            blockNode
            showLine
            selectable={false}
            expandAction="click"
            treeData={nodes}
            expandedKeys={searching ? groups : expanded}
            onExpand={(keys) => setExpanded(keys as number[])}
            titleRender={row}
          />
        ) : (
          <EmptyState title={t('common.noMatchTitle')} description={t('common.noMatchText')} />
        )}
      </div>
    </section>
  );
}
