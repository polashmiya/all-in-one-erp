import { Modal } from 'antd';
import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { sizes } from '../../theme/design-tokens';
import { ActionGroup } from '../actions/ActionGroup';
import type { ActionItem } from '../actions/types';

export type OverlaySize = 'sm' | 'md' | 'lg';

export interface AppModalProps {
  open: boolean;
  onClose: () => void;
  title: ReactNode;
  subtitle?: ReactNode;
  size?: OverlaySize;
  actions?: ActionItem[];
  onOk?: () => unknown;
  okText?: string;
  okLoading?: boolean;
  okDisabled?: boolean;
  danger?: boolean;
  hideFooter?: boolean;
  children: ReactNode;
}

export function AppModal({
  open,
  onClose,
  title,
  subtitle,
  size = 'md',
  actions,
  onOk,
  okText,
  okLoading,
  okDisabled,
  danger,
  hideFooter,
  children,
}: AppModalProps) {
  const { t } = useTranslation();
  const footer: ActionItem[] = actions ?? [
    { key: 'cancel', label: t('common.cancel'), variant: 'text', onClick: onClose },
    {
      key: 'ok',
      label: okText ?? t('common.save'),
      variant: danger ? 'danger' : 'primary',
      onClick: onOk,
      loading: okLoading,
      disabled: okDisabled,
      hidden: !onOk,
    },
  ];

  return (
    <Modal
      open={open}
      onCancel={onClose}
      width={sizes.modal[size]}
      centered
      destroyOnHidden
      className="erp-modal"
      classNames={{ body: 'erp-scroll' }}
      title={
        <div className="erp-overlay__title">
          <span>{title}</span>
          {subtitle && <span className="erp-overlay__sub">{subtitle}</span>}
        </div>
      }
      footer={hideFooter ? null : <ActionGroup actions={footer} className="erp-overlay__footer" />}
    >
      {children}
    </Modal>
  );
}
