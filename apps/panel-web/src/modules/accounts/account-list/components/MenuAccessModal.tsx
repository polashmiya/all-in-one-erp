import { AppModal } from '@erp/ui';
import { Alert } from 'antd';
import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { FeatureAccessTree } from '../../components/FeatureAccess';
import { useSaveFeatures } from '../../components/useSaveFeatures';

export function MenuAccessModal({
  accountId,
  accountName,
  initial,
  open,
  onClose,
}: {
  accountId: string;
  accountName: string;
  initial: string[];
  open: boolean;
  onClose: () => void;
}) {
  const { t } = useTranslation();
  const [codes, setCodes] = useState(initial);
  const features = useSaveFeatures(accountId);

  useEffect(() => {
    if (open) setCodes(initial);
  }, [open, initial]);

  const save = () =>
    features
      .save(codes)
      .then(onClose)
      .catch(() => undefined);

  return (
    <AppModal
      open={open}
      onClose={onClose}
      size="lg"
      title={t('menus.editTitle', { account: accountName })}
      okText={t('menus.save')}
      okLoading={features.saving}
      onOk={save}
    >
      <div className="erp-stack">
        <Alert type="info" showIcon message={t('menus.adminNote')} />
        <FeatureAccessTree value={codes} onChange={setCodes} />
        {codes.length === 0 && <Alert type="warning" showIcon message={t('menus.noneWarning')} />}
      </div>
    </AppModal>
  );
}
