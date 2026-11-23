import { Field, FormModal } from '@erp/ui';
import { Input } from 'antd';
import { useTranslation } from 'react-i18next';
import { REVIEW_COMMENT_MAX } from '../constants';

export type ReviewMode = 'approve' | 'reject';

export function ReviewModal({
  mode,
  code,
  onClose,
  onSubmit,
}: {
  mode?: ReviewMode;
  code: string;
  onClose: () => void;
  onSubmit: (comment: string | null) => Promise<unknown>;
}) {
  const { t } = useTranslation();
  const rejecting = mode === 'reject';

  return (
    <FormModal<{ comment?: string }>
      open={!!mode}
      onClose={onClose}
      title={t(rejecting ? 'requisition.rejectTitle' : 'requisition.approveTitle', { code })}
      subtitle={t(rejecting ? 'requisition.rejectText' : 'requisition.approveText')}
      submitText={t(rejecting ? 'requisition.reject' : 'requisition.approve')}
      danger={rejecting}
      onSubmit={(values) => onSubmit(values.comment?.trim() || null)}
    >
      <Field
        name="comment"
        label={t(rejecting ? 'requisition.reason' : 'requisition.comment')}
        rules={
          rejecting
            ? [{ required: true, whitespace: true, message: t('requisition.reasonRequired') }]
            : []
        }
      >
        <Input.TextArea
          rows={3}
          maxLength={REVIEW_COMMENT_MAX}
          showCount
          placeholder={t(
            rejecting ? 'requisition.reasonPlaceholder' : 'requisition.commentPlaceholder',
          )}
        />
      </Field>
    </FormModal>
  );
}
