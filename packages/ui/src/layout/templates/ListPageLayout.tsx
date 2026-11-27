import { FilterFilled, FilterOutlined } from '@ant-design/icons';
import { Button, Tooltip, type InputRef, type TableColumnsType } from 'antd';
import { useRef, useState, type Key, type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { useLocation } from 'react-router';
import type { ActionItem } from '../../components/actions/types';
import { useRunAction } from '../../components/actions/useRunAction';
import { useHotkey } from '../../hooks/useHotkey';
import { FilterSelect, type FilterOption } from '../../components/dropdown/FilterSelect';
import { formatNumber } from '../../utils/format';
import { DateFilter } from '../../components/filters/DateFilter';
import { SearchInput } from '../../components/filters/filters';
import { DataTable, type DataTableEmpty } from '../../components/table/DataTable';
import { RowActions } from '../../components/table/RowActions';
import { rowActionsWidth, type RowAction } from '../../components/table/row-actions';
import { SERIAL_COLUMN_KEY, SERIAL_COLUMN_WIDTH } from '../../components/table/constants';
import type { ListFilters, ListSort, ListState } from '../../hooks/useListState';
import { PageContainer } from '../page/PageContainer';
import { DRAFT_VIEW, type ListDrafts } from './drafts';
import { DraftTable } from './DraftTable';

export interface ListFilterDef<F> {
  key: Extract<keyof F, string>;
  label: string;
  options?: FilterOption[];
  type?: 'date';
  render?: (value: string | null, onChange: (value: string | null) => void) => ReactNode;
}

export interface ListViewDef<V extends string> {
  key: V;
  label: string;
  count?: number;
}

export interface ListPageLayoutProps<
  T extends object,
  F extends ListFilters,
  V extends string,
  S extends string = string,
> {
  title?: ReactNode;
  subtitle?: ReactNode;
  actions?: ActionItem[];
  summary?: ReactNode;
  state: ListState<F, V, S>;
  search?: { placeholder: string; label: string };
  filters?: ListFilterDef<F>[];
  views?: ListViewDef<V>[];
  columns: TableColumnsType<T>;
  rowActions?: (row: T) => RowAction[];
  rows?: T[];
  rowKey: Extract<keyof T, string> | ((row: T) => Key);
  total?: number;
  loading?: boolean;
  selectable?: boolean;
  bulkActions?: (keys: Key[]) => ActionItem[];
  empty?: DataTableEmpty;
  onRowClick?: (row: T) => void;
  onRowHover?: (row: T) => void;
  drafts?: ListDrafts;
  extra?: ReactNode;
  body?: ReactNode;
}

const SEARCH_KEY = '/';

function viewLabel({ label, count }: { label: string; count?: number }) {
  return (
    <>
      {label}
      {count !== undefined && <span className="erp-quickview__count">{formatNumber(count)}</span>}
    </>
  );
}

export function ListPageLayout<
  T extends object,
  F extends ListFilters,
  V extends string,
  S extends string = string,
>({
  title,
  subtitle,
  actions,
  summary,
  state,
  search,
  filters = [],
  views,
  columns,
  rowActions,
  rows,
  rowKey,
  total = 0,
  loading,
  selectable = false,
  bulkActions,
  empty,
  onRowClick,
  onRowHover,
  drafts,
  extra,
  body,
}: ListPageLayoutProps<T, F, V, S>) {
  const { t } = useTranslation();
  const { pathname } = useLocation();
  const [filtersOpen, setFiltersOpen] = useState(false);
  const searchRef = useRef<InputRef>(null);
  const runAction = useRunAction();
  const newAction = actions?.find((a) => a.key === 'new' && !a.hidden && !a.disabled);

  useHotkey(SEARCH_KEY, () => searchRef.current?.focus(), { enabled: !!search });
  useHotkey('n', () => newAction && void runAction(newAction), { enabled: !!newAction });

  const serialColumn: TableColumnsType<T>[number] = {
    key: SERIAL_COLUMN_KEY,
    title: t('table.sl'),
    width: SERIAL_COLUMN_WIDTH,
    align: 'center',
    render: (_value, _row, index) => (state.page - 1) * state.pageSize + index + 1,
  };
  const actionColumns: TableColumnsType<T> = rowActions
    ? [
        {
          key: 'actions',
          title: t('table.actions'),
          align: 'end',
          fixed: 'right',
          width: rowActionsWidth(rows?.[0] && rowActions(rows[0])),
          render: (_value, row) => <RowActions actions={rowActions(row)} />,
        },
      ]
    : [];
  const filteredEmpty: DataTableEmpty = {
    title: t('common.noMatchTitle'),
    description: t('common.noMatchText'),
    actions: [{ key: 'reset', label: t('common.reset'), onClick: state.reset }],
  };

  const allViews =
    drafts && views
      ? [...views, { key: DRAFT_VIEW as V, label: t('common.drafts'), count: drafts.rows?.length }]
      : views;
  const showDrafts = !!drafts && state.view === DRAFT_VIEW;
  const [defaultView, ...otherViews] = (state.view !== undefined && allViews) || [];

  const viewFilter = defaultView && (
    <FilterSelect
      label={t('common.listView')}
      allLabel={viewLabel(defaultView)}
      options={otherViews.map((view) => ({ value: view.key, label: viewLabel(view) }))}
      value={state.view === defaultView.key ? null : state.view}
      onChange={(next) => state.setView((next ?? defaultView.key) as V)}
    />
  );
  const hasPanel = !body && (!!viewFilter || filters.length > 0);

  const filterRow = filtersOpen && hasPanel && (
    <div className="erp-table__filters">
      {viewFilter}
      {!showDrafts &&
        filters.map((filter) => {
          const value = (state.filters[filter.key] as string | null | undefined) ?? null;
          const change = (next: string | null) =>
            state.setFilter(filter.key, next as F[typeof filter.key]);
          if (filter.render) return <div key={filter.key}>{filter.render(value, change)}</div>;
          const Control = filter.type === 'date' ? DateFilter : FilterSelect;
          return (
            <Control
              key={filter.key}
              label={filter.label}
              options={filter.options ?? []}
              value={value}
              onChange={change}
            />
          );
        })}
    </div>
  );

  const headerTools = (
    <>
      {extra}
      {search && (
        <SearchInput
          ref={searchRef}
          label={search.label}
          placeholder={search.placeholder}
          shortcut={SEARCH_KEY}
          value={state.search}
          onChange={state.setSearch}
        />
      )}
      {hasPanel && (
        <Tooltip title={filtersOpen ? t('common.hideFilters') : t('common.showFilters')}>
          <Button
            icon={filtersOpen ? <FilterFilled /> : <FilterOutlined />}
            aria-label={filtersOpen ? t('common.hideFilters') : t('common.showFilters')}
            aria-expanded={filtersOpen}
            onClick={() => setFiltersOpen((open) => !open)}
          />
        </Tooltip>
      )}
      {state.hasActiveFilters && (
        <button type="button" className="erp-linkbtn" onClick={state.reset}>
          {t('common.reset')}
        </button>
      )}
    </>
  );

  return (
    <PageContainer fill title={title} subTitle={subtitle} actions={actions} extra={headerTools}>
      {summary}
      {body ? (
        body
      ) : showDrafts ? (
        <DraftTable drafts={drafts} header={filterRow} />
      ) : (
        <DataTable<T>
          fill
          header={filterRow}
          settingsKey={pathname}
          defaultHidden={[SERIAL_COLUMN_KEY]}
          rowKey={rowKey}
          columns={[serialColumn, ...columns, ...actionColumns]}
          dataSource={rows}
          loading={loading}
          sort={state.sort}
          onSortChange={(sort) => state.setSort(sort as ListSort<S> | undefined)}
          selection={
            selectable ? { selectedKeys: state.selected, onChange: state.setSelected } : undefined
          }
          bulkActions={bulkActions}
          empty={state.hasActiveFilters ? filteredEmpty : empty}
          onRow={(row) => ({
            onClick: onRowClick && (() => onRowClick(row)),
            onMouseEnter: onRowHover && (() => onRowHover(row)),
            className: onRowClick ? 'is-clickable' : undefined,
          })}
          pagination={{
            page: state.page,
            pageSize: state.pageSize,
            total,
            onChange: state.changePage,
          }}
        />
      )}
    </PageContainer>
  );
}
