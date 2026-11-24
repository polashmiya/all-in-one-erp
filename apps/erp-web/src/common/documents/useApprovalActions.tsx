import {
  CheckCircleOutlined,
  CloseCircleOutlined,
  DeleteOutlined,
  StopOutlined,
  LockOutlined,
} from '@ant-design/icons';
import { problemMessage, type ApprovalProgress, type ReviewRequest } from '@erp/api-client';
import type { RowAction } from '@erp/ui';
import type { QueryKey } from '@tanstack/react-query';
import { App } from 'antd';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { ReviewModal, type ReviewMode } from '@/common/approval/ReviewModal';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { useRefreshDocuments } from './documents';

export interface ApprovalTarget {
  id: number;
  code: string;
  status: string;
  isManual?: boolean;
}

export interface ApprovalCalls {
  approve: (id: number, data: ReviewRequest) => Promise<unknown>;
  reject: (id: number, data: ReviewRequest) => Promise<unknown>;
  close?: (id: number) => Promise<unknown>;
  cancel: (id: number, data: ReviewRequest) => Promise<unknown>;
  canCancel?: (target: ApprovalTarget) => boolean;
  remove: (id: number) => Promise<unknown>;
  progress: (id: number) => Promise<ApprovalProgress>;
  progressKey: (id: number) => QueryKey;
}

export function useApprovalActions(
  feature: string,
  calls: ApprovalCalls,
  onDone?: (action: string) => void,
) {
  const { t } = useTranslation();
  const { message } = App.useApp();
  const access = useFeatureAccess(feature);
  const refresh = useRefreshDocuments();
  const [review, setReview] = useState<{ target: ApprovalTarget; mode: ReviewMode }>();

  const run = async (key: string, target: ApprovalTarget, action: () => Promise<unknown>) => {
    try {
      await action();
      void message.success(t(`document.done.${key}`, { code: target.code }));
      await refresh();
      onDone?.(key);
    } catch (error) {
      void message.error(problemMessage(error, t('master.failed')));
      throw error;
    }
  };

  const actions = (target: ApprovalTarget, canAct = false): RowAction[] => [
    {
      key: 'approve',
      label: t('approval.approve'),
      icon: <CheckCircleOutlined />,
      hidden: !access.approve || !canAct,
      onClick: () => setReview({ target, mode: 'approve' }),
    },
    {
      key: 'reject',
      label: t('approval.reject'),
      icon: <CloseCircleOutlined />,
      hidden: !access.approve || !canAct,
      onClick: () => setReview({ target, mode: 'reject' }),
    },
    {
      key: 'close',
      label: t('document.close'),
      icon: <LockOutlined />,
      hidden: !calls.close || !access.edit || target.status !== 'approved',
      confirm: {
        title: t('document.closeConfirm', { code: target.code }),
        description: t('document.closeHint'),
        okText: t('document.close'),
      },
      onClick: () => run('closed', target, async () => calls.close?.(target.id)),
    },
    {
      key: 'cancel',
      label: t('document.cancel'),
      icon: <StopOutlined />,
      danger: true,
      hidden: !access.cancel || target.status !== 'approved' || calls.canCancel?.(target) === false,
      onClick: () => setReview({ target, mode: 'cancel' }),
    },
    {
      key: 'delete',
      label: t('document.delete'),
      icon: <DeleteOutlined />,
      danger: true,
      hidden: !access.delete || !['pending', 'rejected'].includes(target.status),
      confirm: {
        title: t('document.deleteConfirm', { code: target.code }),
        description: t('document.deleteHint'),
        okText: t('document.delete'),
        danger: true,
      },
      onClick: () => run('deleted', target, () => calls.remove(target.id)),
    },
  ];

  const modal = (
    <ReviewModal
      mode={review?.mode}
      open={!!review}
      code={review?.target.code ?? ''}
      onClose={() => setReview(undefined)}
      onSubmit={(comment) => {
        if (!review) return Promise.resolve();
        const { target, mode } = review;
        const done = { approve: 'approved', reject: 'rejected', cancel: 'cancelled' }[mode];
        return run(done, target, () => calls[mode](target.id, { comment }));
      }}
    />
  );

  return { actions, modal };
}
