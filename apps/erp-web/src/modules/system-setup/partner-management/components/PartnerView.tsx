import {
  EditOutlined,
  EnvironmentOutlined,
  IdcardOutlined,
  PhoneOutlined,
  WalletOutlined,
} from '@ant-design/icons';
import {
  problemMessage,
  type CustomerDetail,
  type FeatureConfig,
  type SupplierDetail,
} from '@erp/api-client';
import { useViewTabs } from '@erp/metadata-engine';
import {
  DescriptionList,
  DetailSection,
  DetailsPageLayout,
  formatDateTime,
  formatMoney,
  StatusTag,
  type Tint,
} from '@erp/ui';
import { App } from 'antd';
import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { useCustomLookups } from '@/common/hooks/useCustomLookups';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { useRefreshPartners, type PartnerPaths } from '../partner-management';

type Partner = CustomerDetail | SupplierDetail;

export interface PartnerViewProps {
  partner: Partner;
  config: FeatureConfig;
  feature: string;
  paths: PartnerPaths;
  icon: ReactNode;
  tint: Tint;
  onSetStatus: (isActive: boolean) => Promise<unknown>;
}

export function PartnerView({
  partner,
  config,
  feature,
  paths,
  icon,
  tint,
  onSetStatus,
}: PartnerViewProps) {
  const { t } = useTranslation();
  const { message } = App.useApp();
  const access = useFeatureAccess(feature);
  const refresh = useRefreshPartners();
  const tabs = useViewTabs(config, partner, {}, useCustomLookups(config));

  const toggleStatus = async () => {
    try {
      await onSetStatus(!partner.isActive);
      await refresh();
      void message.success(
        t(partner.isActive ? 'master.deactivated' : 'master.activated', { count: 1 }),
      );
    } catch (error) {
      void message.error(problemMessage(error, t('master.failed')));
    }
  };

  const contact =
    partner.mobile || partner.email ? (
      <>
        {partner.mobile && <a href={`tel:${partner.mobile}`}>{partner.mobile}</a>}
        {partner.mobile && partner.email && ' · '}
        {partner.email && <a href={`mailto:${partner.email}`}>{partner.email}</a>}
      </>
    ) : (
      t('partner.noContact')
    );
  const place = [partner.city, partner.thanaName, partner.districtName].filter(Boolean).join(', ');
  const credit = [
    partner.paymentDays
      ? t('partner.creditDays', { count: partner.paymentDays })
      : t('partner.cashOnly'),
    partner.creditLimit != null &&
      t('partner.creditLimit', { amount: formatMoney(partner.creditLimit) }),
  ]
    .filter(Boolean)
    .join(' · ');

  const recordInfo = (
    <DetailSection title={t('partner.side.recordInfo')}>
      <DescriptionList
        columns={3}
        items={[
          {
            key: 'created',
            label: t('partner.side.created'),
            value: formatDateTime(partner.createdAt),
          },
          {
            key: 'updated',
            label: t('partner.side.updated'),
            value: partner.modifiedAt ? formatDateTime(partner.modifiedAt) : null,
          },
        ]}
      />
    </DetailSection>
  );

  return (
    <DetailsPageLayout
      crumbs={[partner.code]}
      header={{
        backTo: paths.list,
        icon,
        tint,
        title: partner.name,
        tags: (
          <>
            <StatusTag tone={partner.isActive ? 'success' : 'neutral'} dot>
              {partner.isActive ? t('master.active') : t('master.inactive')}
            </StatusTag>
            {partner.groupName && <StatusTag tone="violet">{partner.groupName}</StatusTag>}
            {partner.isForeign && <StatusTag tone="info">{t('partner.foreign')}</StatusTag>}
          </>
        ),
        meta: [
          {
            key: 'code',
            icon: <IdcardOutlined />,
            label: [partner.code, partner.billingName].filter(Boolean).join(' · '),
          },
          {
            key: 'contact',
            icon: <PhoneOutlined />,
            label: contact,
          },
          {
            key: 'place',
            icon: <EnvironmentOutlined />,
            label: place || t('partner.noAddress'),
          },
          { key: 'credit', icon: <WalletOutlined />, label: credit },
        ],
        actions: [
          {
            key: 'edit',
            label: t('master.edit'),
            icon: <EditOutlined />,
            variant: 'primary',
            hidden: !access.edit,
            to: paths.edit(partner.id),
          },
          {
            key: 'more',
            label: t('common.moreActions'),
            hidden: !access.edit,
            menu: [
              {
                key: 'toggle',
                label: partner.isActive ? t('master.deactivate') : t('master.activate'),
                onClick: toggleStatus,
              },
            ],
          },
        ],
      }}
      tabs={tabs.map((tab, index) =>
        index === tabs.length - 1
          ? { ...tab, sections: [...tab.sections, { key: 'record', content: recordInfo }] }
          : tab,
      )}
    />
  );
}
