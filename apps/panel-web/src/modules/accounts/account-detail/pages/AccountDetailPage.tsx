import {
  ApartmentOutlined,
  BankOutlined,
  BgColorsOutlined,
  EditOutlined,
  EnvironmentOutlined,
  HistoryOutlined,
  InboxOutlined,
  LayoutOutlined,
  MailOutlined,
  SettingOutlined,
  ShopOutlined,
  UnorderedListOutlined,
} from '@ant-design/icons';
import { getGetAccountQueryKey, useGetAccount } from '@erp/api-client';
import {
  DescriptionList,
  DetailSection,
  DetailsPageLayout,
  DotValue,
  EmptyState,
  formatDateTime,
  StatusTag,
  useDisclosure,
  useLanguageStore,
} from '@erp/ui';
import { useQueryClient } from '@tanstack/react-query';
import { Button, Typography } from 'antd';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate, useParams } from 'react-router';
import { MenuAccessModal } from '../../account-list/components/MenuAccessModal';
import { FeatureAccessSummary } from '../../components/FeatureAccess';
import { structureCounts, structureFromDetail } from '../../components/structure';
import { StructureView, type StructureTarget } from '../../components/StructureView';
import { ACCOUNT_PATHS } from '../../constants';
import { countryName } from '../../utils/format';
import { StructureNodeModal } from '../components/StructureNodeModal';

export function AccountDetailPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const language = useLanguageStore((s) => s.language);
  const id = useParams().id ?? '';
  const queryClient = useQueryClient();
  const query = useGetAccount(id);
  const menus = useDisclosure();
  const [target, setTarget] = useState<StructureTarget>();
  const account = query.data;

  if (!account)
    return (
      <DetailsPageLayout
        loading={query.isPending}
        missing={!query.isPending}
        missingTitle={t('detail.loadFailed')}
        missingActions={[{ key: 'back', label: t('accounts.title'), to: ACCOUNT_PATHS.list }]}
      />
    );

  const refresh = () => queryClient.invalidateQueries({ queryKey: getGetAccountQueryKey(id) });
  const units = structureFromDetail(account.businessUnits);
  const counts = structureCounts(units);

  const profile = (
    <DetailSection title={t('detail.profile')}>
      <DescriptionList
        columns={3}
        items={[
          {
            key: 'email',
            label: t('field.email'),
            value: account.email && <a href={`mailto:${account.email}`}>{account.email}</a>,
          },
          { key: 'phone', label: t('field.phone'), value: account.phone },
          {
            key: 'website',
            label: t('field.website'),
            value: account.website && (
              <a href={account.website} target="_blank" rel="noreferrer">
                {account.website}
              </a>
            ),
          },
          { key: 'industry', label: t('field.industry'), value: account.industry },
          { key: 'tax', label: t('field.taxId'), value: account.taxId },
          { key: 'currency', label: t('field.baseCurrency'), value: account.baseCurrency },
          { key: 'address', label: t('field.address'), value: account.address },
          { key: 'tz', label: t('field.timeZone'), value: account.timeZone },
          { key: 'created', label: t('detail.created'), value: formatDateTime(account.createdAt) },
        ]}
      />
    </DetailSection>
  );

  const admin = (
    <DetailSection title={t('detail.admin')}>
      {account.admin ? (
        <DescriptionList
          columns={3}
          items={[
            { key: 'name', label: t('detail.adminName'), value: account.admin.fullName },
            {
              key: 'email',
              label: t('field.email'),
              value: (
                <Typography.Text copyable={{ text: account.admin.email }}>
                  {account.admin.email}
                </Typography.Text>
              ),
            },
            {
              key: 'login',
              label: t('detail.lastLogin'),
              value: account.admin.lastLoginAt
                ? formatDateTime(account.admin.lastLoginAt)
                : t('detail.never'),
            },
          ]}
        />
      ) : (
        <EmptyState title={t('detail.noAdmin')} />
      )}
    </DetailSection>
  );

  const structure = (
    <DetailSection
      title={t('detail.structure')}
      extra={
        <span className="erp-inline-stats">
          <DotValue tone="primary" label={t('accounts.stat.businessUnits')}>
            <ApartmentOutlined /> {counts.units}
          </DotValue>
          <DotValue tone="info" label={t('accounts.stat.offices')}>
            <ShopOutlined /> {counts.offices}
          </DotValue>
          <DotValue tone="warning" label={t('accounts.stat.warehouses')}>
            <InboxOutlined /> {counts.warehouses}
          </DotValue>
          <Button icon={<ApartmentOutlined />} onClick={() => setTarget({ kind: 'business-unit' })}>
            {t('structure.add.unit')}
          </Button>
        </span>
      }
    >
      <StructureView units={units} onAdd={setTarget} onEdit={setTarget} />
    </DetailSection>
  );

  const access = (
    <DetailSection
      title={t('menus.title')}
      extra={
        <Button type="link" icon={<EditOutlined />} onClick={() => menus.onOpen()}>
          {t('menus.edit')}
        </Button>
      }
    >
      <FeatureAccessSummary codes={account.featureCodes} />
    </DetailSection>
  );

  const go = (path: string) => () => navigate(path);

  return (
    <>
      <DetailsPageLayout
        crumbs={[account.name]}
        header={{
          backTo: ACCOUNT_PATHS.list,
          icon: <BankOutlined />,
          title: account.name,
          tags: (
            <>
              <StatusTag>{account.code}</StatusTag>
              <StatusTag tone={account.isActive ? 'success' : 'neutral'} dot>
                {account.isActive ? t('accounts.status.active') : t('accounts.status.inactive')}
              </StatusTag>
            </>
          ),
          meta: [
            {
              key: 'place',
              icon: <EnvironmentOutlined />,
              label: [account.industry, account.city, countryName(account.country, language)]
                .filter(Boolean)
                .join(' · '),
            },
            { key: 'mail', icon: <MailOutlined />, label: account.email ?? '—' },
          ],
          actions: [
            {
              key: 'edit',
              label: t('detail.edit'),
              icon: <EditOutlined />,
              variant: 'primary',
              to: ACCOUNT_PATHS.edit(id),
            },
            {
              key: 'more',
              label: t('common.moreActions'),
              menu: [
                {
                  key: 'screens',
                  label: t('config.action'),
                  icon: <SettingOutlined />,
                  onClick: go(ACCOUNT_PATHS.configuration(id)),
                },
                {
                  key: 'layout',
                  label: t('layout.action'),
                  icon: <LayoutOutlined />,
                  onClick: go(ACCOUNT_PATHS.layout(id)),
                },
                {
                  key: 'lookups',
                  label: t('lookups.action'),
                  icon: <UnorderedListOutlined />,
                  onClick: go(ACCOUNT_PATHS.lookups(id)),
                },
                {
                  key: 'theme',
                  label: t('theme.action'),
                  icon: <BgColorsOutlined />,
                  onClick: go(ACCOUNT_PATHS.theme(id)),
                },
                {
                  key: 'history',
                  label: t('history.action'),
                  icon: <HistoryOutlined />,
                  onClick: go(ACCOUNT_PATHS.history(id)),
                },
              ],
            },
          ],
        }}
        tabs={[
          {
            key: 'overview',
            label: t('detail.tab.overview'),
            sections: [
              { key: 'profile', content: profile },
              { key: 'admin', content: admin },
            ],
          },
          {
            key: 'structure',
            label: t('detail.structure'),
            sections: [{ key: 'structure', content: structure }],
          },
          { key: 'menus', label: t('menus.title'), sections: [{ key: 'menus', content: access }] },
        ]}
      />
      <StructureNodeModal
        key={`${target?.kind}-${target?.node?.key ?? target?.unitKey ?? ''}-${target?.officeKey ?? ''}`}
        accountId={id}
        target={target}
        units={units}
        onClose={() => setTarget(undefined)}
        onSaved={refresh}
      />
      <MenuAccessModal
        accountId={account.id}
        accountName={account.name}
        initial={account.featureCodes}
        open={menus.open}
        onClose={menus.onClose}
      />
    </>
  );
}
