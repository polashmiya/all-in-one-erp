import { Field, FormModal } from '@erp/ui';
import { Input } from 'antd';
import { useTranslation } from 'react-i18next';
import { REVIEW_COMMENT_MAX } from './approval';

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
      title={t(rejecting ? 'approval.review.rejectTitle' : 'approval.review.approveTitle', {
        code,
      })}
      subtitle={t(rejecting ? 'approval.review.rejectText' : 'approval.review.approveText')}
      submitText={t(rejecting ? 'approval.reject' : 'approval.approve')}
      danger={rejecting}
      onSubmit={(values) => onSubmit(values.comment?.trim() || null)}
    >
      <Field
        name="comment"
        label={t(rejecting ? 'approval.review.reason' : 'approval.review.comment')}
        rules={
          rejecting
            ? [
                {
                  required: true,
                  whitespace: true,
                  message: t('approval.review.reasonRequired'),
                },
              ]
            : []
        }
      >
        <Input.TextArea
          rows={3}
          maxLength={REVIEW_COMMENT_MAX}
          showCount
          placeholder={t(
            rejecting ? 'approval.review.reasonPlaceholder' : 'approval.review.commentPlaceholder',
          )}
        />
      </Field>
    </FormModal>
  );
}
