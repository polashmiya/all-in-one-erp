import { Drawer, Grid } from 'antd';
import {
  useCallback,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
  type SyntheticEvent,
} from 'react';
import { useLocation, useNavigation } from 'react-router';
import { ErrorBoundary } from '../../components/feedback/ResultPages';
import { useHotkey } from '../../hooks/useHotkey';
import { useThemeStore } from '../../theme/theme-store';
import { findTrail, type MenuNode } from '../../utils/menu';
import { CommandPalette, ShortcutsModal } from './CommandPalette';
import type { ShellUser } from './ProfileMenu';
import { useRecentPages } from './recent-pages';
import { ShellContext, usePreloadRoute } from './ShellContext';
import { Sidebar, type SidebarWorkspace } from './Sidebar';
import { Topbar } from './Topbar';

export interface AppShellProps {
  workspace: SidebarWorkspace;
  menu: MenuNode[];
  footerMenu?: MenuNode[];
  menuLoading?: boolean;
  user: ShellUser;
  businessUnit?: ReactNode;
  notifications?: number;
  version?: string;
  onProfile?: () => void;
  onChangePassword?: () => void;
  onResetPassword?: () => void;
  onLogout?: () => void;
  children: ReactNode;
}

export function AppShell({
  workspace,
  menu,
  footerMenu = [],
  menuLoading = false,
  user,
  businessUnit,
  notifications,
  version,
  onProfile,
  onChangePassword,
  onResetPassword,
  onLogout,
  children,
}: AppShellProps) {
  const { pathname } = useLocation();
  const navigating = useNavigation().state === 'loading';
  const preload = usePreloadRoute();
  const screens = Grid.useBreakpoint();
  const mobile = screens.md === false;
  const rail = useThemeStore((s) => s.siderCollapsed);
  const setRail = useThemeStore((s) => s.setSiderCollapsed);
  const [drawerOpen, setDrawerOpen] = useState(false);
  const [searchOpen, setSearchOpen] = useState(false);
  const [shortcutsOpen, setShortcutsOpen] = useState(false);
  const [themeOpen, setThemeOpen] = useState(false);
  const [crumbs, setCrumbs] = useState<string[]>([]);

  const allMenu = useMemo(() => [...menu, ...footerMenu], [menu, footerMenu]);
  const trail = useMemo(() => findTrail(allMenu, pathname), [allMenu, pathname]);
  const stableSetCrumbs = useCallback((next: string[]) => setCrumbs(next), []);
  const shell = useMemo(
    () => ({ menu: allMenu, trail, setCrumbs: stableSetCrumbs }),
    [allMenu, trail, stableSetCrumbs],
  );

  useHotkey('k', () => setSearchOpen(true), { mod: true });
  useHotkey('[', () => setRail(!rail), { enabled: !mobile });
  useHotkey('?', () => setShortcutsOpen(true));

  const visit = useRecentPages((s) => s.visit);
  const currentPage = trail.at(-1)?.path;
  useEffect(() => {
    if (currentPage) visit(currentPage);
  }, [currentPage, visit]);

  const preloadLink = (event: SyntheticEvent) => {
    const href = (event.target as Element).closest('a[href^="/"]')?.getAttribute('href');
    if (href) preload(href);
  };

  const sidebar = (inDrawer: boolean) => (
    <Sidebar
      workspace={workspace}
      menu={menu}
      footerMenu={footerMenu}
      loading={menuLoading}
      version={version}
      rail={!inDrawer && rail}
      collapsible={!inDrawer}
      onToggleRail={() => setRail(!rail)}
      onSearch={() => {
        setDrawerOpen(false);
        setSearchOpen(true);
      }}
      onNavigate={inDrawer ? () => setDrawerOpen(false) : undefined}
    />
  );

  return (
    <ShellContext.Provider value={shell}>
      <div className="erp-app" onPointerOver={preloadLink} onFocus={preloadLink}>
        {mobile ? (
          <Drawer
            open={drawerOpen}
            onClose={() => setDrawerOpen(false)}
            placement="left"
            width={280}
            closable={false}
            styles={{
              body: { padding: 0, background: 'var(--sb-bg)', display: 'flex' },
              wrapper: { boxShadow: 'none' },
            }}
          >
            {sidebar(true)}
          </Drawer>
        ) : (
          sidebar(false)
        )}
        <div className="erp-main">
          <div className="erp-panel">
            {navigating && <div className="erp-progress" />}
            <Topbar
              crumbs={crumbs}
              user={user}
              businessUnit={businessUnit}
              notifications={notifications}
              themeOpen={themeOpen}
              onThemeOpenChange={setThemeOpen}
              onShortcuts={() => setShortcutsOpen(true)}
              onProfile={onProfile}
              onChangePassword={onChangePassword}
              onResetPassword={onResetPassword}
              onLogout={onLogout}
              onOpenMenu={mobile ? () => setDrawerOpen(true) : undefined}
            />
            <main className="erp-content erp-scroll">
              <ErrorBoundary key={pathname}>{children}</ErrorBoundary>
            </main>
          </div>
        </div>
      </div>
      <CommandPalette open={searchOpen} menu={allMenu} onClose={() => setSearchOpen(false)} />
      <ShortcutsModal open={shortcutsOpen} onClose={() => setShortcutsOpen(false)} />
    </ShellContext.Provider>
  );
}
