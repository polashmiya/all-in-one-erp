import { Field, FormModal } from '@erp/ui';
import { Input } from 'antd';
import { useTranslation } from 'react-i18next';
import { REVIEW_COMMENT_MAX } from '../procurement';

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
      title={t(rejecting ? 'procurement.review.rejectTitle' : 'procurement.review.approveTitle', {
        code,
      })}
      subtitle={t(rejecting ? 'procurement.review.rejectText' : 'procurement.review.approveText')}
      submitText={t(rejecting ? 'procurement.reject' : 'procurement.approve')}
      danger={rejecting}
      onSubmit={(values) => onSubmit(values.comment?.trim() || null)}
    >
      <Field
        name="comment"
        label={t(rejecting ? 'procurement.review.reason' : 'procurement.review.comment')}
        rules={
          rejecting
            ? [
                {
                  required: true,
                  whitespace: true,
                  message: t('procurement.review.reasonRequired'),
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
            rejecting
              ? 'procurement.review.reasonPlaceholder'
              : 'procurement.review.commentPlaceholder',
          )}
        />
      </Field>
    </FormModal>
  );
}
