import { HistoryOutlined, SearchOutlined } from '@ant-design/icons';
import { Modal } from 'antd';
import { useEffect, useMemo, useState, type KeyboardEvent } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router';
import { AppModal } from '../../components/overlays/AppModal';
import { useLanguageStore } from '../../i18n/i18n';
import { localize } from '../../i18n/language';
import { flattenLeavesWithTrail, type MenuNode } from '../../utils/menu';
import { useRecentPages } from './recent-pages';
import { usePreloadRoute } from './ShellContext';

const MAX_RESULTS = 50;

export function CommandPalette({
  open,
  menu,
  onClose,
}: {
  open: boolean;
  menu: MenuNode[];
  onClose: () => void;
}) {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const language = useLanguageStore((s) => s.language);
  const [query, setQuery] = useState('');
  const [cursor, setCursor] = useState(0);
  const preload = usePreloadRoute();
  const recent = useRecentPages((s) => s.paths);

  const results = useMemo(() => {
    const needle = query.trim().toLocaleLowerCase();
    const rank = (path = '') => {
      const index = needle ? -1 : recent.indexOf(path);
      return index === -1 ? recent.length : index;
    };
    return flattenLeavesWithTrail(menu)
      .map(({ node, ancestors }) => ({
        node,
        label: localize(node.label, language),
        path: ancestors.map((a) => localize(a.label, language)).join(' › '),
        icon: ancestors[0]?.icon ?? node.icon,
        haystack: [node.label, ...ancestors.map((a) => a.label)]
          .flatMap((label) => (typeof label === 'string' ? [label] : [label.en, label.bn ?? '']))
          .join(' ')
          .toLocaleLowerCase(),
      }))
      .filter((r) => !needle || r.haystack.includes(needle))
      .map((r) => ({ ...r, rank: rank(r.node.path) }))
      .sort((a, b) => a.rank - b.rank)
      .slice(0, MAX_RESULTS);
  }, [menu, query, language, recent]);

  const activePath = results[cursor]?.node.path;
  useEffect(() => {
    if (activePath) preload(activePath);
  }, [activePath, preload]);

  const close = () => {
    setQuery('');
    setCursor(0);
    onClose();
  };

  const go = (index: number) => {
    const target = results[index];
    if (!target?.node.path) return;
    navigate(target.node.path);
    close();
  };

  const onKeyDown = (event: KeyboardEvent<HTMLInputElement>) => {
    if (event.key === 'ArrowDown') {
      event.preventDefault();
      setCursor((c) => Math.min(results.length - 1, c + 1));
    } else if (event.key === 'ArrowUp') {
      event.preventDefault();
      setCursor((c) => Math.max(0, c - 1));
    } else if (event.key === 'Enter') {
      event.preventDefault();
      go(cursor);
    }
  };

  return (
    <Modal
      open={open}
      onCancel={close}
      footer={null}
      closable={false}
      width={560}
      className="erp-cmd"
      destroyOnHidden
    >
      <div className="erp-cmd__input">
        <SearchOutlined />
        <input
          autoFocus
          role="combobox"
          aria-expanded="true"
          aria-controls="erp-cmd-results"
          aria-activedescendant={
            results[cursor] ? `erp-cmd-${results[cursor]!.node.key}` : undefined
          }
          aria-label={t('shell.search')}
          placeholder={t('shell.searchJump')}
          value={query}
          onChange={(e) => {
            setQuery(e.target.value);
            setCursor(0);
          }}
          onKeyDown={onKeyDown}
        />
        <kbd className="erp-kbd">Esc</kbd>
      </div>
      {results.length === 0 ? (
        <div className="erp-cmd__empty">{t('shell.noMenuMatch')}</div>
      ) : (
        <ul id="erp-cmd-results" role="listbox" className="erp-cmd__list erp-scroll">
          {results.map((r, index) => (
            <li
              key={r.node.key}
              id={`erp-cmd-${r.node.key}`}
              role="option"
              aria-selected={index === cursor}
              className="erp-cmd__item"
              onMouseEnter={() => setCursor(index)}
              onClick={() => go(index)}
            >
              <span className="erp-cmd__tile">
                {r.rank < recent.length ? <HistoryOutlined /> : r.icon}
              </span>
              <span className="erp-stack is-tight">
                <span className="erp-cmd__label">{r.label}</span>
                {r.path && <span className="erp-cmd__path">{r.path}</span>}
              </span>
            </li>
          ))}
        </ul>
      )}
      <div className="erp-cmd__foot">
        <span>
          <kbd className="erp-kbd">↑</kbd>
          <kbd className="erp-kbd">↓</kbd>
          {t('shell.cmdNavigate')}
        </span>
        <span>
          <kbd className="erp-kbd">Enter</kbd>
          {t('shell.cmdOpen')}
        </span>
      </div>
    </Modal>
  );
}

export function ShortcutsModal({ open, onClose }: { open: boolean; onClose: () => void }) {
  const { t } = useTranslation();
  const rows: [string[], string][] = [
    [['Ctrl', 'K'], t('shell.shortcutSearch')],
    [['['], t('shell.shortcutSidebar')],
    [['/'], t('shell.shortcutFind')],
    [['N'], t('shell.shortcutNew')],
    [['Ctrl', 'S'], t('shell.shortcutSave')],
    [['?'], t('shell.shortcutHelp')],
    [['Esc'], t('shell.shortcutClose')],
  ];

  return (
    <AppModal open={open} onClose={onClose} title={t('shell.shortcuts')} size="sm" hideFooter>
      <dl className="erp-shortcuts">
        {rows.map(([keys, label]) => (
          <div key={label} className="erp-contents">
            <dt>{label}</dt>
            <dd>
              {keys.map((key) => (
                <kbd key={key} className="erp-kbd">
                  {key}
                </kbd>
              ))}
            </dd>
          </div>
        ))}
      </dl>
    </AppModal>
  );
}
