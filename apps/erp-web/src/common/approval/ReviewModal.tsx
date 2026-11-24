import { Field, FormModal } from '@erp/ui';
import { Input } from 'antd';
import { useTranslation } from 'react-i18next';
import { REVIEW_COMMENT_MAX } from './approval';

export type ReviewMode = 'approve' | 'reject' | 'cancel';

export function ReviewModal({
  mode = 'approve',
  open,
  code,
  onClose,
  onSubmit,
}: {
  mode?: ReviewMode;
  open: boolean;
  code: string;
  onClose: () => void;
  onSubmit: (comment: string | null) => Promise<unknown>;
}) {
  const { t } = useTranslation();
  const needsReason = mode !== 'approve';

  return (
    <FormModal<{ comment?: string }>
      open={open}
      onClose={onClose}
      title={t(`approval.review.${mode}Title`, {
        code,
      })}
      subtitle={t(`approval.review.${mode}Text`)}
      submitText={t(`approval.${mode}`)}
      danger={needsReason}
      onSubmit={(values) => onSubmit(values.comment?.trim() || null)}
    >
      <Field
        name="comment"
        label={t(needsReason ? 'approval.review.reason' : 'approval.review.comment')}
        rules={
          needsReason
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
            needsReason
              ? 'approval.review.reasonPlaceholder'
              : 'approval.review.commentPlaceholder',
          )}
        />
      </Field>
    </FormModal>
  );
}
