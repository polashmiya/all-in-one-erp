import { AppstoreOutlined, ControlOutlined, EyeOutlined } from '@ant-design/icons';
import { LoginLayout } from '@erp/ui';
import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { appConfig } from '@/common/config/app.config';

export function AuthLayout({ children }: { children: ReactNode }) {
  const { t } = useTranslation();

  return (
    <LoginLayout
      appName={appConfig.name}
      announcement={t('login.announcement')}
      headline={t('login.headline')}
      description={t('login.description')}
      preview={{
        stats: [t('login.previewStat1'), t('login.previewStat2'), t('login.previewStat3')],
        publishedTitle: t('login.previewPublished'),
        publishedMeta: t('login.previewPublishedMeta'),
        roleNote: t('login.previewRole'),
      }}
      features={[
        { icon: <ControlOutlined />, title: t('login.point1'), description: t('login.point1Text') },
        {
          icon: <AppstoreOutlined />,
          title: t('login.point2'),
          description: t('login.point2Text'),
        },
        { icon: <EyeOutlined />, title: t('login.point3'), description: t('login.point3Text') },
      ]}
      footerNote={t('login.footer', { year: new Date().getFullYear(), app: appConfig.name })}
    >
      {children}
    </LoginLayout>
  );
}
