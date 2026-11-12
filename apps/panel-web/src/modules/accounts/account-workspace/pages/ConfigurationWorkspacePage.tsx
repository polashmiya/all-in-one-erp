import {
  ArrowRightOutlined,
  BgColorsOutlined,
  HistoryOutlined,
  LayoutOutlined,
  SettingOutlined,
  UnorderedListOutlined,
} from '@ant-design/icons';
import { useListAccounts } from '@erp/api-client';
import { EmptyState, FilterSelect, PageContainer } from '@erp/ui';
import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useSearchParams } from 'react-router';
import { ACCOUNT_PATHS, ACCOUNT_PICKER_SIZE, WORKSPACE_PARAM } from '../../constants';

interface Tool {
  key: string;
  icon: ReactNode;
  tint: string;
  path: (id: string) => string;
}

const TOOLS: Tool[] = [
  { key: 'screens', icon: <SettingOutlined />, tint: 'primary', path: ACCOUNT_PATHS.configuration },
  { key: 'layout', icon: <LayoutOutlined />, tint: 'violet', path: ACCOUNT_PATHS.layout },
  { key: 'lookups', icon: <UnorderedListOutlined />, tint: 'blue', path: ACCOUNT_PATHS.lookups },
  { key: 'theme', icon: <BgColorsOutlined />, tint: 'amber', path: ACCOUNT_PATHS.theme },
  { key: 'history', icon: <HistoryOutlined />, tint: 'slate', path: ACCOUNT_PATHS.history },
];

export function ConfigurationWorkspacePage() {
  const { t } = useTranslation();
  const [search, setSearch] = useSearchParams();
  const accounts = useListAccounts({
    status: 'active',
    sort: 'name',
    pageSize: ACCOUNT_PICKER_SIZE,
  });
  const options = (accounts.data?.items ?? []).map((a) => ({
    value: a.id,
    label: `${a.name} (${a.code})`,
  }));
  const accountId = search.get(WORKSPACE_PARAM) ?? options[0]?.value;

  return (
    <PageContainer title={t('workspace.title')} subTitle={t('workspace.subtitle')}>
      <div className="acc-hub">
        <FilterSelect
          label={t('workspace.account')}
          options={options}
          value={accountId}
          onChange={(value) =>
            setSearch(value ? { [WORKSPACE_PARAM]: value } : {}, { replace: true })
          }
        />
        {accountId ? (
          <div className="acc-hub__grid">
            {TOOLS.map((tool) => (
              <Link key={tool.key} to={tool.path(accountId)} className="erp-card acc-hub__tool">
                <span className={`erp-tile erp-tile--${tool.tint}`}>{tool.icon}</span>
                <span className="acc-hub__text">
                  <span className="acc-hub__name">{t(`workspace.tool.${tool.key}`)}</span>
                  <span className="acc-hub__hint">{t(`workspace.hint.${tool.key}`)}</span>
                </span>
                <ArrowRightOutlined className="acc-hub__arrow" />
              </Link>
            ))}
          </div>
        ) : (
          !accounts.isPending && <EmptyState title={t('workspace.noAccounts')} />
        )}
      </div>
    </PageContainer>
  );
}
