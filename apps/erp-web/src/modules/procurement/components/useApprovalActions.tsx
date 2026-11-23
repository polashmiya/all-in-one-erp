import {
  CheckCircleOutlined,
  CloseCircleOutlined,
  DeleteOutlined,
  LockOutlined,
} from '@ant-design/icons';
import { problemMessage, type ReviewRequest } from '@erp/api-client';
import type { RowAction } from '@erp/ui';
import { App } from 'antd';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { useRefreshProcurement } from '../procurement';
import { ReviewModal, type ReviewMode } from './ReviewModal';

interface Target {
  id: number;
  code: string;
  status: string;
}

export interface ApprovalCalls {
  approve: (id: number, data: ReviewRequest) => Promise<unknown>;
  reject: (id: number, data: ReviewRequest) => Promise<unknown>;
  close: (id: number) => Promise<unknown>;
  remove: (id: number) => Promise<unknown>;
}

export function useApprovalActions(
  feature: string,
  calls: ApprovalCalls,
  onDone?: (action: string) => void,
) {
  const { t } = useTranslation();
  const { message } = App.useApp();
  const access = useFeatureAccess(feature);
  const refresh = useRefreshProcurement();
  const [review, setReview] = useState<{ target: Target; mode: ReviewMode }>();

  const run = async (key: string, target: Target, action: () => Promise<unknown>) => {
    try {
      await action();
      void message.success(t(`procurement.done.${key}`, { code: target.code }));
      await refresh();
      onDone?.(key);
    } catch (error) {
      void message.error(problemMessage(error, t('master.failed')));
      throw error;
    }
  };

  const actions = (target: Target): RowAction[] => [
    {
      key: 'approve',
      label: t('procurement.approve'),
      icon: <CheckCircleOutlined />,
      hidden: !access.approve || target.status !== 'pending',
      onClick: () => setReview({ target, mode: 'approve' }),
    },
    {
      key: 'reject',
      label: t('procurement.reject'),
      icon: <CloseCircleOutlined />,
      hidden: !access.approve || target.status !== 'pending',
      onClick: () => setReview({ target, mode: 'reject' }),
    },
    {
      key: 'close',
      label: t('procurement.close'),
      icon: <LockOutlined />,
      hidden: !access.edit || target.status !== 'approved',
      confirm: {
        title: t('procurement.closeConfirm', { code: target.code }),
        description: t('procurement.closeHint'),
        okText: t('procurement.close'),
      },
      onClick: () => run('closed', target, () => calls.close(target.id)),
    },
    {
      key: 'delete',
      label: t('procurement.delete'),
      icon: <DeleteOutlined />,
      danger: true,
      hidden: !access.delete || !['pending', 'rejected'].includes(target.status),
      confirm: {
        title: t('procurement.deleteConfirm', { code: target.code }),
        description: t('procurement.deleteHint'),
        okText: t('procurement.delete'),
        danger: true,
      },
      onClick: () => run('deleted', target, () => calls.remove(target.id)),
    },
  ];

  const modal = (
    <ReviewModal
      mode={review?.mode}
      code={review?.target.code ?? ''}
      onClose={() => setReview(undefined)}
      onSubmit={(comment) => {
        if (!review) return Promise.resolve();
        const { target, mode } = review;
        return mode === 'approve'
          ? run('approved', target, () => calls.approve(target.id, { comment }))
          : run('rejected', target, () => calls.reject(target.id, { comment }));
      }}
    />
  );

  return { actions, modal };
}
