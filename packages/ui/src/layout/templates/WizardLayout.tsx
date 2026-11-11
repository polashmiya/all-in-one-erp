import { ArrowLeftOutlined, ArrowRightOutlined, CheckOutlined } from '@ant-design/icons';
import { Grid, Steps } from 'antd';
import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { ActionBar, PageContainer } from '../page/PageContainer';
import { SplitLayout } from '../page/CardGrid';

export interface WizardStep {
  key: string;
  title: ReactNode;
  description?: ReactNode;
  icon?: ReactNode;
}

export interface WizardLayoutProps {
  title: ReactNode;
  subTitle?: ReactNode;
  crumbs?: string[];
  cancelTo?: string;
  steps: WizardStep[];
  current: number;
  reached?: number;
  onStepChange: (index: number) => void;
  finishLabel: string;
  finishing?: boolean;
  onFinish: () => void;
  aside?: ReactNode;
  children: ReactNode;
}

export function WizardLayout({
  title,
  subTitle,
  crumbs,
  cancelTo,
  steps,
  current,
  reached = current,
  onStepChange,
  finishLabel,
  finishing,
  onFinish,
  aside,
  children,
}: WizardLayoutProps) {
  const { t } = useTranslation();
  const screens = Grid.useBreakpoint();
  const rail = screens.lg !== false;
  const last = current === steps.length - 1;

  const stepper = (
    <Steps
      current={current}
      size="small"
      direction={rail || screens.md === false ? 'vertical' : 'horizontal'}
      onChange={onStepChange}
      items={steps.map((step, index) => ({ ...step, disabled: index > reached }))}
    />
  );

  const main = (
    <section className="erp-card erp-form-card">
      {!rail && <div className="erp-wizard__steps">{stepper}</div>}
      <div className="erp-wizard__body">{children}</div>
      <ActionBar
        status={t('common.stepOf', { current: current + 1, total: steps.length })}
        actions={[
          {
            key: 'back',
            label: t('common.back'),
            icon: <ArrowLeftOutlined />,
            disabled: current === 0,
            onClick: () => onStepChange(current - 1),
          },
          last
            ? {
                key: 'finish',
                label: finishLabel,
                icon: <CheckOutlined />,
                variant: 'primary',
                loading: finishing,
                onClick: onFinish,
              }
            : {
                key: 'next',
                label: t('common.next'),
                icon: <ArrowRightOutlined />,
                variant: 'primary',
                onClick: () => onStepChange(current + 1),
              },
        ]}
      />
    </section>
  );

  return (
    <PageContainer
      title={title}
      subTitle={subTitle}
      crumbs={crumbs}
      actions={cancelTo ? [{ key: 'cancel', label: t('common.cancel'), to: cancelTo }] : undefined}
    >
      {rail ? (
        <div className="erp-wizard">
          <div className="erp-wizard__rail">
            <section className="erp-card erp-wizard__steps">{stepper}</section>
            {aside}
          </div>
          {main}
        </div>
      ) : (
        <SplitLayout aside={aside} main={main} />
      )}
    </PageContainer>
  );
}
