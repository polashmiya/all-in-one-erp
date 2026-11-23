import { CheckOutlined, ClockCircleOutlined, CloseOutlined } from '@ant-design/icons';
import type { ApprovalStepView } from '@erp/api-client';
import { DetailSection, formatDateTime, StatusTag } from '@erp/ui';
import { useTranslation } from 'react-i18next';
import { STEP_TONES } from './approval';
import './approval.css';

function stateOf(step: ApprovalStepView) {
  if (step.status === 'approved') return 'is-approved';
  if (step.status === 'rejected') return 'is-rejected';
  return step.isCurrent ? 'is-current' : 'is-waiting';
}

function Marker({ step }: { step: ApprovalStepView }) {
  if (step.status === 'approved') return <CheckOutlined />;
  if (step.status === 'rejected') return <CloseOutlined />;
  return step.isCurrent ? <ClockCircleOutlined /> : <>{step.level}</>;
}

export function ApprovalSteps({ steps }: { steps: ApprovalStepView[] }) {
  const { t } = useTranslation();
  const done = steps.filter((step) => step.status === 'approved').length;
  const statusLabel = (step: ApprovalStepView) =>
    step.status === 'pending'
      ? t(step.isCurrent ? 'approval.step.current' : 'approval.step.waiting')
      : t(`approval.step.${step.status}`);

  return (
    <DetailSection
      title={t('approval.title')}
      description={t('approval.progress', { done, total: steps.length })}
    >
      <ol className="erp-steps">
        {steps.map((step) => (
          <li key={step.level} className={`erp-steps__item ${stateOf(step)}`}>
            <span className="erp-steps__marker" aria-hidden="true">
              <Marker step={step} />
            </span>
            <div className="erp-steps__body">
              <div className="erp-steps__head">
                <span className="erp-steps__title">
                  {t('approval.levelName', { level: step.level, name: step.name })}
                </span>
                <StatusTag
                  tone={step.isCurrent ? 'warning' : (STEP_TONES[step.status] ?? 'neutral')}
                  dot
                >
                  {statusLabel(step)}
                </StatusTag>
              </div>
              <span className="erp-steps__meta">
                {step.actedByName
                  ? t('approval.actedBy', {
                      name: step.actedByName,
                      date: formatDateTime(step.actedAt),
                    })
                  : t('approval.approver', {
                      name: step.approverName ?? t('approval.anyApprover'),
                    })}
              </span>
              {step.comment && <p className="erp-steps__comment">{step.comment}</p>}
            </div>
          </li>
        ))}
      </ol>
    </DetailSection>
  );
}
