import {
  CloseOutlined,
  DoubleLeftOutlined,
  DoubleRightOutlined,
  PushpinOutlined,
  RightOutlined,
  SearchOutlined,
  StarFilled,
  StarOutlined,
} from '@ant-design/icons';
import { Popover, Skeleton, Tooltip } from 'antd';
import { useMemo, useState, type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router';
import { BrandMark } from '../../components/brand/BrandMark';
import { useLanguageStore } from '../../i18n/i18n';
import { localize } from '../../i18n/language';
import { tintOrder } from '../../theme/design-tokens';
import { flattenLeaves, type MenuNode } from '../../utils/menu';
import { useFavoritesStore } from './favorites-store';
import { useHomePath, useShell } from './ShellContext';

export interface SidebarWorkspace {
  name: string;
  subtitle?: string;
}

export interface SidebarProps {
  workspace: SidebarWorkspace;
  menu: MenuNode[];
  footerMenu?: MenuNode[];
  loading?: boolean;
  version?: string;
  rail: boolean;
  collapsible?: boolean;
  onToggleRail: () => void;
  onSearch: () => void;
  onNavigate?: () => void;
}

const FAVORITE_TINTS = tintOrder.filter((tint) => tint !== 'slate');
const FLYOUT_LEAVE_DELAY = 0.2;

const cx = (...names: (string | false | undefined)[]) => names.filter(Boolean).join(' ');

interface OpenChoice {
  leaf?: string;
  module?: string | null;
  sub?: string | null;
}

function Collapse({ open, children }: { open: boolean; children: ReactNode }) {
  return (
    <div className={cx('erp-sb-collapse', open && 'is-open')} inert={!open}>
      <div className="erp-sb-collapse__inner">{children}</div>
    </div>
  );
}

function useLabel() {
  const language = useLanguageStore((s) => s.language);
  return (node: MenuNode) => localize(node.label, language);
}

function LeafRow({
  node,
  active,
  onNavigate,
}: {
  node: MenuNode;
  active: boolean;
  onNavigate?: () => void;
}) {
  const { t } = useTranslation();
  const label = useLabel();
  const favorite = useFavoritesStore((s) => s.keys.includes(node.key));
  const toggleFavorite = useFavoritesStore((s) => s.toggle);

  return (
    <div className={cx('erp-sb-leafrow', active && 'is-active')}>
      <Link
        to={node.path!}
        className="erp-sb-leaf"
        aria-current={active ? 'page' : undefined}
        onClick={onNavigate}
      >
        <span className="erp-sb-text">{label(node)}</span>
        {node.count !== undefined && <span className="erp-sb-count">{node.count}</span>}
        {node.badge !== undefined && <span className="erp-sb-badge">{node.badge}</span>}
      </Link>
      <button
        type="button"
        className={cx('erp-sb-pin', favorite && 'is-on')}
        aria-pressed={favorite}
        aria-label={favorite ? t('shell.removeFavorite') : t('shell.addFavorite')}
        onClick={() => toggleFavorite(node.key)}
      >
        {favorite ? <StarFilled /> : <StarOutlined />}
      </button>
    </div>
  );
}

function TopRow({
  node,
  active,
  onNavigate,
}: {
  node: MenuNode;
  active: boolean;
  onNavigate?: () => void;
}) {
  const label = useLabel();

  return (
    <Link
      to={node.path!}
      className={cx('erp-sb-row', active && 'is-active')}
      aria-current={active ? 'page' : undefined}
      onClick={onNavigate}
    >
      <span className="erp-sb-icon">{node.icon}</span>
      <span className="erp-sb-text">{label(node)}</span>
      {node.badge !== undefined && <span className="erp-sb-badge">{node.badge}</span>}
    </Link>
  );
}

function Flyout({
  module,
  activeKey,
  onClose,
  onPin,
}: {
  module: MenuNode;
  activeKey?: string;
  onClose: () => void;
  onPin: () => void;
}) {
  const { t } = useTranslation();
  const label = useLabel();

  return (
    <nav className="erp-fly" aria-label={label(module)}>
      <div className="erp-fly__head">
        <span className="erp-fly__tile">{module.icon}</span>
        <span className="erp-fly__title">{label(module)}</span>
        <button
          type="button"
          className="erp-menu-item"
          aria-label={t('shell.close')}
          onClick={onClose}
        >
          <CloseOutlined />
        </button>
      </div>
      <div className="erp-fly__list">
        {(module.children ?? []).map((sub) =>
          sub.children ? (
            <div key={sub.key}>
              <div className="erp-fly__section">{label(sub)}</div>
              {sub.children
                .filter((leaf) => leaf.path)
                .map((leaf) => (
                  <Link
                    key={leaf.key}
                    to={leaf.path!}
                    className={cx('erp-fly__item', leaf.key === activeKey && 'is-active')}
                    aria-current={leaf.key === activeKey ? 'page' : undefined}
                    onClick={onClose}
                  >
                    <span className="erp-sb-text">{label(leaf)}</span>
                    {leaf.count !== undefined && <span className="erp-sb-count">{leaf.count}</span>}
                  </Link>
                ))}
            </div>
          ) : sub.path ? (
            <Link
              key={sub.key}
              to={sub.path}
              className={cx('erp-fly__item', sub.key === activeKey && 'is-active')}
              onClick={onClose}
            >
              <span className="erp-sb-text">{label(sub)}</span>
            </Link>
          ) : null,
        )}
      </div>
      <button type="button" className="erp-fly__pin" onClick={onPin}>
        <PushpinOutlined />
        {t('shell.pinOpen')}
      </button>
    </nav>
  );
}

export function Sidebar({
  workspace,
  menu,
  footerMenu = [],
  loading = false,
  version,
  rail,
  collapsible = true,
  onToggleRail,
  onSearch,
  onNavigate,
}: SidebarProps) {
  const { t } = useTranslation();
  const label = useLabel();
  const homePath = useHomePath();
  const { trail } = useShell();
  const favoriteKeys = useFavoritesStore((s) => s.keys);
  const [choice, setChoice] = useState<OpenChoice>({});
  const [flyout, setFlyout] = useState<string | null>(null);

  const activeKeys = useMemo(() => new Set(trail.map((n) => n.key)), [trail]);
  const activeLeaf = trail.at(-1)?.key;
  const topItems = menu.filter((n) => !n.children?.length && n.path);
  const modules = menu.filter((n) => n.children?.length);
  const favorites = useMemo(() => {
    const leaves = flattenLeaves(menu);
    return favoriteKeys
      .map((key) => leaves.find((leaf) => leaf.key === key))
      .filter((leaf): leaf is MenuNode => !!leaf);
  }, [favoriteKeys, menu]);

  const [activeModule = null, activeSub = null] = trail
    .filter((n) => n.children?.length)
    .map((n) => n.key);
  const current = choice.leaf === activeLeaf ? choice : {};
  const openModule = current.module === undefined ? activeModule : current.module;
  const openSub = current.sub === undefined ? activeSub : current.sub;
  const choose = (next: OpenChoice) => setChoice({ ...current, ...next, leaf: activeLeaf });
  const toggleModule = (key: string) => choose({ module: openModule === key ? null : key });
  const toggleSub = (key: string) => choose({ sub: openSub === key ? null : key });

  const pinOpen = (moduleKey: string) => {
    setFlyout(null);
    choose({ module: moduleKey });
    onToggleRail();
  };

  const search = (
    <kbd className="erp-kbd" aria-hidden="true">
      Ctrl K
    </kbd>
  );

  if (rail) {
    return (
      <aside className="erp-sider is-rail" aria-label={t('shell.mainNav')}>
        <div className="erp-sb-head">
          <Link to={homePath} aria-label={workspace.name} onClick={onNavigate}>
            <BrandMark size={32} />
          </Link>
        </div>
        <nav className="erp-rail-nav">
          <Tooltip title={t('shell.expand')} placement="right">
            <button
              type="button"
              className="erp-rail-btn is-muted"
              aria-label={t('shell.expand')}
              onClick={onToggleRail}
            >
              <DoubleRightOutlined />
            </button>
          </Tooltip>
          <Tooltip title={`${t('shell.search')} · Ctrl K`} placement="right">
            <button
              type="button"
              className="erp-rail-btn"
              aria-label={t('shell.search')}
              onClick={onSearch}
            >
              <SearchOutlined />
            </button>
          </Tooltip>
          {topItems.length > 0 && <span className="erp-rail-div" />}
          {topItems.map((node) => (
            <Tooltip key={node.key} title={label(node)} placement="right">
              <Link
                to={node.path!}
                className={cx('erp-rail-btn', node.key === activeLeaf && 'is-active')}
                aria-label={label(node)}
                aria-current={node.key === activeLeaf ? 'page' : undefined}
                onClick={onNavigate}
              >
                {node.icon}
              </Link>
            </Tooltip>
          ))}
          {modules.length > 0 && <span className="erp-rail-div" />}
          {modules.map((module) => (
            <Popover
              key={module.key}
              trigger="hover"
              placement="rightTop"
              arrow={false}
              align={{ overflow: { shiftY: true } }}
              mouseLeaveDelay={FLYOUT_LEAVE_DELAY}
              open={flyout === module.key}
              onOpenChange={(open) =>
                setFlyout((current) =>
                  open ? module.key : current === module.key ? null : current,
                )
              }
              styles={{ body: { padding: 8 } }}
              content={
                <Flyout
                  module={module}
                  activeKey={activeLeaf}
                  onClose={() => setFlyout(null)}
                  onPin={() => pinOpen(module.key)}
                />
              }
            >
              <button
                type="button"
                className={cx('erp-rail-btn', activeKeys.has(module.key) && 'is-active')}
                aria-label={label(module)}
                aria-expanded={flyout === module.key}
                onClick={() => setFlyout(module.key)}
              >
                {module.icon}
              </button>
            </Popover>
          ))}
        </nav>
        {footerMenu.length > 0 && (
          <div className="erp-sb-foot">
            {footerMenu.map((node) => (
              <Tooltip key={node.key} title={label(node)} placement="right">
                <Link
                  to={node.path!}
                  className={cx('erp-rail-btn', node.key === activeLeaf && 'is-active')}
                  aria-label={label(node)}
                  onClick={onNavigate}
                >
                  {node.icon}
                </Link>
              </Tooltip>
            ))}
          </div>
        )}
      </aside>
    );
  }

  return (
    <aside className="erp-sider" aria-label={t('shell.mainNav')}>
      <div className="erp-sb-head">
        <Link to={homePath} className="erp-ws" onClick={onNavigate}>
          <BrandMark size={28} />
          <span className="erp-ws__text">
            <span className="erp-ws__name">{workspace.name}</span>
            {workspace.subtitle && <span className="erp-ws__sub">{workspace.subtitle}</span>}
          </span>
        </Link>
        {collapsible && (
          <Tooltip title={`${t('shell.collapse')} · [`} placement="right">
            <button
              type="button"
              className="erp-sb-iconbtn"
              aria-label={t('shell.collapse')}
              onClick={onToggleRail}
            >
              <DoubleLeftOutlined />
            </button>
          </Tooltip>
        )}
      </div>

      <div className="erp-sb-search-wrap">
        <button
          type="button"
          className="erp-sb-search"
          aria-label={`${t('shell.search')}, Control K`}
          onClick={onSearch}
        >
          <SearchOutlined />
          <span>{t('shell.searchJump')}</span>
          {search}
        </button>
      </div>

      <nav className="erp-sb-nav">
        {topItems.map((node) => (
          <TopRow
            key={node.key}
            node={node}
            active={node.key === activeLeaf}
            onNavigate={onNavigate}
          />
        ))}

        {favorites.length > 0 && (
          <>
            <div className="erp-sb-label">{t('shell.favorites')}</div>
            {favorites.map((node, index) => (
              <div
                key={node.key}
                className={cx('erp-sb-leafrow erp-sb-fav', node.key === activeLeaf && 'is-active')}
              >
                <Link
                  to={node.path!}
                  className="erp-sb-row"
                  aria-current={node.key === activeLeaf ? 'page' : undefined}
                  onClick={onNavigate}
                >
                  <span
                    className="erp-sb-dot"
                    data-tint={FAVORITE_TINTS[index % FAVORITE_TINTS.length]}
                  />
                  <span className="erp-sb-text">{label(node)}</span>
                </Link>
                <FavoriteRemove nodeKey={node.key} />
              </div>
            ))}
          </>
        )}

        {(loading || modules.length > 0) && (
          <div className="erp-sb-label">{t('shell.modules')}</div>
        )}
        {loading && (
          <Skeleton active title={false} paragraph={{ rows: 5 }} className="erp-sb-skeleton" />
        )}

        {modules.map((module) => {
          const open = openModule === module.key;
          return (
            <div key={module.key}>
              <button
                type="button"
                className={cx('erp-sb-row', activeKeys.has(module.key) && 'has-active')}
                aria-expanded={open}
                onClick={() => toggleModule(module.key)}
              >
                <span className="erp-sb-icon">{module.icon}</span>
                <span className="erp-sb-text">{label(module)}</span>
                <span className={cx('erp-sb-chev', open && 'is-open')}>
                  <RightOutlined />
                </span>
              </button>
              <Collapse open={open}>
                <div className="erp-sb-tree">
                  {(module.children ?? []).map((sub) =>
                    sub.children?.length ? (
                      <div key={sub.key}>
                        <button
                          type="button"
                          className={cx(
                            'erp-sb-row erp-sb-sub',
                            activeKeys.has(sub.key) && 'has-active',
                          )}
                          aria-expanded={openSub === sub.key}
                          onClick={() => toggleSub(sub.key)}
                        >
                          <span className="erp-sb-text">{label(sub)}</span>
                          <span className={cx('erp-sb-chev', openSub === sub.key && 'is-open')}>
                            <RightOutlined />
                          </span>
                        </button>
                        <Collapse open={openSub === sub.key}>
                          <div className="erp-sb-tree">
                            {sub.children
                              .filter((leaf) => leaf.path)
                              .map((leaf) => (
                                <LeafRow
                                  key={leaf.key}
                                  node={leaf}
                                  active={leaf.key === activeLeaf}
                                  onNavigate={onNavigate}
                                />
                              ))}
                          </div>
                        </Collapse>
                      </div>
                    ) : sub.path ? (
                      <LeafRow
                        key={sub.key}
                        node={sub}
                        active={sub.key === activeLeaf}
                        onNavigate={onNavigate}
                      />
                    ) : null,
                  )}
                </div>
              </Collapse>
            </div>
          );
        })}
      </nav>

      {(footerMenu.length > 0 || version) && (
        <div className="erp-sb-foot">
          {footerMenu.map((node, index) => (
            <Link
              key={node.key}
              to={node.path!}
              className={cx('erp-sb-row', node.key === activeLeaf && 'is-active')}
              aria-current={node.key === activeLeaf ? 'page' : undefined}
              onClick={onNavigate}
            >
              <span className="erp-sb-icon">{node.icon}</span>
              <span className="erp-sb-text">{label(node)}</span>
              {version && index === footerMenu.length - 1 && (
                <span className="erp-sb-version">v{version}</span>
              )}
            </Link>
          ))}
          {footerMenu.length === 0 && version && (
            <span className="erp-sb-row is-static">
              <span className="erp-sb-version">v{version}</span>
            </span>
          )}
        </div>
      )}
    </aside>
  );
}

function FavoriteRemove({ nodeKey }: { nodeKey: string }) {
  const { t } = useTranslation();
  const toggle = useFavoritesStore((s) => s.toggle);

  return (
    <button
      type="button"
      className="erp-sb-pin is-on"
      aria-label={t('shell.removeFavorite')}
      onClick={() => toggle(nodeKey)}
    >
      <StarFilled />
    </button>
  );
}
