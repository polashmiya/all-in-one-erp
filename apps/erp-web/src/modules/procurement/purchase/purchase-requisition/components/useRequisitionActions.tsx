import {
  CheckCircleOutlined,
  CloseCircleOutlined,
  DeleteOutlined,
  LockOutlined,
} from '@ant-design/icons';
import {
  problemMessage,
  useApprovePurchaseRequisition,
  useClosePurchaseRequisition,
  useDeletePurchaseRequisition,
  useRejectPurchaseRequisition,
} from '@erp/api-client';
import type { RowAction } from '@erp/ui';
import { App } from 'antd';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useFeatureAccess } from '@/common/hooks/useFeatureAccess';
import { useRefreshProcurement } from '../../../procurement';
import { REQUISITION_FEATURE } from '../constants';
import { ReviewModal, type ReviewMode } from './ReviewModal';

interface Target {
  id: number;
  code: string;
  status: string;
}

export function useRequisitionActions(onDone?: (action: string) => void) {
  const { t } = useTranslation();
  const { message } = App.useApp();
  const access = useFeatureAccess(REQUISITION_FEATURE);
  const refresh = useRefreshProcurement();
  const approve = useApprovePurchaseRequisition();
  const reject = useRejectPurchaseRequisition();
  const close = useClosePurchaseRequisition();
  const remove = useDeletePurchaseRequisition();
  const [review, setReview] = useState<{ target: Target; mode: ReviewMode }>();

  const run = async (key: string, target: Target, action: () => Promise<unknown>) => {
    try {
      await action();
      void message.success(t(`requisition.${key}`, { code: target.code }));
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
      label: t('requisition.approve'),
      icon: <CheckCircleOutlined />,
      hidden: !access.approve || target.status !== 'pending',
      onClick: () => setReview({ target, mode: 'approve' }),
    },
    {
      key: 'reject',
      label: t('requisition.reject'),
      icon: <CloseCircleOutlined />,
      hidden: !access.approve || target.status !== 'pending',
      onClick: () => setReview({ target, mode: 'reject' }),
    },
    {
      key: 'close',
      label: t('requisition.close'),
      icon: <LockOutlined />,
      hidden: !access.edit || target.status !== 'approved',
      confirm: {
        title: t('requisition.closeConfirm', { code: target.code }),
        description: t('requisition.closeHint'),
        okText: t('requisition.close'),
      },
      onClick: () => run('closed', target, () => close.mutateAsync({ id: target.id })),
    },
    {
      key: 'delete',
      label: t('requisition.delete'),
      icon: <DeleteOutlined />,
      danger: true,
      hidden: !access.delete || !['pending', 'rejected'].includes(target.status),
      confirm: {
        title: t('requisition.deleteConfirm', { code: target.code }),
        description: t('requisition.deleteHint'),
        okText: t('requisition.delete'),
        danger: true,
      },
      onClick: () => run('deleted', target, () => remove.mutateAsync({ id: target.id })),
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
        const data = { comment };
        return mode === 'approve'
          ? run('approved', target, () => approve.mutateAsync({ id: target.id, data }))
          : run('rejected', target, () => reject.mutateAsync({ id: target.id, data }));
      }}
    />
  );

  return { actions, modal };
}
