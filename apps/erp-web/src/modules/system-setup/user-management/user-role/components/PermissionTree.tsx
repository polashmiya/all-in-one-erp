import { AppstoreOutlined, RightOutlined, SearchOutlined } from '@ant-design/icons';
import type { LocalizedLabel, PermissionTreeFeature, PermissionTreeModule } from '@erp/api-client';
import { EmptyState, localize, resolveIcon, useLanguageStore } from '@erp/ui';
import { Button, Checkbox, Input, Segmented, Skeleton, Tag } from 'antd';
import { useMemo, useState, type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { STANDARD_ACTIONS } from '../constants';
import {
  checkState,
  featuresOf,
  filterTree,
  keysOf,
  otherActions,
  permissionKey,
  setAction,
  setAll,
  type CheckState,
} from '../permission-tree';

type Show = 'all' | 'selected';

function Highlight({ text, query }: { text: string; query: string }) {
  const at = query ? text.toLowerCase().indexOf(query) : -1;
  if (at < 0) return <>{text}</>;
  return (
    <>
      {text.slice(0, at)}
      <mark>{text.slice(at, at + query.length)}</mark>
      {text.slice(at + query.length)}
    </>
  );
}

function TriCheck({
  state,
  label,
  disabled,
  onChange,
}: {
  state: CheckState;
  label: string;
  disabled?: boolean;
  onChange: (value: boolean) => void;
}) {
  return (
    <Checkbox
      aria-label={label}
      checked={state === 'on'}
      indeterminate={state === 'mix'}
      disabled={disabled}
      onChange={() => onChange(state !== 'on')}
    />
  );
}

export function PermissionTree({
  modules,
  value,
  onChange,
  loading,
  readOnly,
}: {
  modules: PermissionTreeModule[];
  value?: string[];
  onChange?: (codes: string[]) => void;
  loading?: boolean;
  readOnly?: boolean;
}) {
  const { t } = useTranslation();
  const language = useLanguageStore((s) => s.language);
  const [query, setQuery] = useState('');
  const [show, setShow] = useState<Show>('all');
  const [closed, setClosed] = useState<Set<string>>(new Set());
  const selected = useMemo(() => new Set(value ?? []), [value]);
  const label = (text: LocalizedLabel) =>
    localize({ en: text.en, ...(text.bn ? { bn: text.bn } : {}) }, language);
  const actionLabel = (action: string) => t(`role.action.${action}`, { defaultValue: action });
  const words = query.trim().toLowerCase();
  const searching = !!words || show === 'selected';

  const visible = filterTree(
    modules,
    (feature, names) =>
      (show === 'all' || keysOf([feature]).some((key) => selected.has(key))) &&
      (!words ||
        [...names, ...otherActions(feature).map(actionLabel)].some((name) =>
          name.toLowerCase().includes(words),
        )),
    label,
  );
  const total = keysOf(featuresOf(modules)).length;
  const isOpen = (key: string) => searching || !closed.has(key);
  const toggle = (key: string) =>
    setClosed((current) => {
      const next = new Set(current);
      if (next.has(key)) next.delete(key);
      else next.add(key);
      return next;
    });
  const emit = (next: Set<string>) => onChange?.([...next].sort());

  const columnCells = (features: PermissionTreeFeature[], scope: string) =>
    STANDARD_ACTIONS.map((action) => {
      const having = features.filter((f) => f.actions.includes(action));
      return (
        <div key={action} className="role-tree__cell">
          {having.length > 0 ? (
            <TriCheck
              state={checkState(
                selected,
                having.map((f) => permissionKey(f, action)),
              )}
              label={`${actionLabel(action)} · ${scope}`}
              disabled={readOnly}
              onChange={(next) => emit(setAction(selected, having, action, next))}
            />
          ) : (
            <span className="role-tree__none">—</span>
          )}
        </div>
      );
    });

  const groupRow = (
    key: string,
    level: 'module' | 'sub',
    name: string,
    icon: ReactNode,
    features: PermissionTreeFeature[],
  ) => {
    const keys = keysOf(features);
    const open = isOpen(key);
    return (
      <div key={key} className={`role-tree__row is-${level}`} role="treeitem" aria-expanded={open}>
        <div className="role-tree__name">
          <Button
            type="text"
            size="small"
            className={`role-tree__chevron${open ? ' is-open' : ''}`}
            icon={<RightOutlined />}
            aria-label={open ? t('role.tree.collapse') : t('role.tree.expand')}
            onClick={() => toggle(key)}
          />
          <TriCheck
            state={checkState(selected, keys)}
            label={name}
            disabled={readOnly}
            onChange={(next) => emit(setAll(selected, features, next))}
          />
          {level === 'module' && <span className="role-tree__icon">{icon}</span>}
          <span className="role-tree__label">
            <Highlight text={name} query={words} />
          </span>
          <Tag bordered={false} className="role-tree__count">
            {keys.filter((k) => selected.has(k)).length}/{keys.length}
          </Tag>
        </div>
        {columnCells(features, name)}
      </div>
    );
  };

  const featureRow = (feature: PermissionTreeFeature) => {
    const name = label(feature.name);
    const others = otherActions(feature);
    return (
      <div key={feature.code} className="role-tree__row is-feature" role="treeitem">
        <div className="role-tree__name">
          <TriCheck
            state={checkState(selected, keysOf([feature]))}
            label={name}
            disabled={readOnly}
            onChange={(next) => emit(setAll(selected, [feature], next))}
          />
          <div className="role-tree__feature">
            <span className="role-tree__label">
              <Highlight text={name} query={words} />
            </span>
            {others.length > 0 && (
              <div className="role-tree__chips">
                {others.map((action) => {
                  const key = permissionKey(feature, action);
                  return (
                    <Tag.CheckableTag
                      key={action}
                      checked={selected.has(key)}
                      onChange={(next) =>
                        !readOnly && emit(setAction(selected, [feature], action, next))
                      }
                    >
                      <Highlight text={actionLabel(action)} query={words} />
                    </Tag.CheckableTag>
                  );
                })}
              </div>
            )}
          </div>
        </div>
        {STANDARD_ACTIONS.map((action) => (
          <div key={action} className="role-tree__cell">
            {feature.actions.includes(action) ? (
              <Checkbox
                aria-label={`${actionLabel(action)} · ${name}`}
                checked={selected.has(permissionKey(feature, action))}
                disabled={readOnly}
                onChange={(event) =>
                  emit(setAction(selected, [feature], action, event.target.checked))
                }
              />
            ) : (
              <span className="role-tree__none">—</span>
            )}
          </div>
        ))}
      </div>
    );
  };

  return (
    <div className="role-tree-card">
      <div className="role-tree__toolbar">
        <Input
          allowClear
          prefix={<SearchOutlined />}
          placeholder={t('role.tree.search')}
          aria-label={t('role.tree.search')}
          value={query}
          onChange={(event) => setQuery(event.target.value)}
          className="role-tree__search"
        />
        <Segmented<Show>
          value={show}
          onChange={setShow}
          options={[
            { value: 'all', label: t('role.tree.all') },
            { value: 'selected', label: t('role.tree.selected') },
          ]}
        />
        <span className="role-tree__tools">
          <Button size="small" onClick={() => setClosed(new Set())}>
            {t('role.tree.expandAll')}
          </Button>
          <Button
            size="small"
            onClick={() =>
              setClosed(
                new Set(
                  modules.flatMap((m) => [m.key, ...m.subModules.map((s) => `${m.key}/${s.key}`)]),
                ),
              )
            }
          >
            {t('role.tree.collapseAll')}
          </Button>
          <Tag color="processing" bordered={false}>
            {t('role.tree.count', { count: selected.size, total })}
          </Tag>
        </span>
      </div>
      {loading ? (
        <Skeleton active paragraph={{ rows: 6 }} title={false} />
      ) : visible.length === 0 ? (
        <EmptyState
          title={t('role.tree.noMatch', { query })}
          actions={
            <Button
              onClick={() => {
                setQuery('');
                setShow('all');
              }}
            >
              {t('role.tree.clear')}
            </Button>
          }
        />
      ) : (
        <div className="role-tree" role="tree" aria-label={t('role.tree.title')}>
          <div className="role-tree__row is-head">
            <div className="role-tree__name">{t('role.tree.screen')}</div>
            {columnCells(featuresOf(visible), t('role.tree.allScreens')).map((cell, index) => (
              <div key={STANDARD_ACTIONS[index]} className="role-tree__head-cell">
                <span>{actionLabel(STANDARD_ACTIONS[index]!)}</span>
                {cell}
              </div>
            ))}
          </div>
          {visible.flatMap((module) => [
            groupRow(
              module.key,
              'module',
              label(module.name),
              resolveIcon(module.icon) ?? <AppstoreOutlined />,
              module.subModules.flatMap((s) => s.features),
            ),
            ...(isOpen(module.key)
              ? module.subModules.flatMap((sub) => {
                  const key = `${module.key}/${sub.key}`;
                  return [
                    groupRow(key, 'sub', label(sub.name), null, sub.features),
                    ...(isOpen(key) ? sub.features.map(featureRow) : []),
                  ];
                })
              : []),
          ])}
        </div>
      )}
    </div>
  );
}
