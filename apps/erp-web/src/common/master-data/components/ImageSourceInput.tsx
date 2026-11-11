import { ImageDrop } from '@erp/ui';
import { App, Spin, type UploadFile } from 'antd';
import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toImageSource } from '../image';

export function ImageSourceInput({
  value,
  onChange,
  disabled,
}: {
  value?: string | null;
  onChange?: (value: string | null) => void;
  disabled?: boolean;
}) {
  const { t } = useTranslation();
  const { message } = App.useApp();
  const [reading, setReading] = useState(false);
  const files = useMemo<UploadFile[]>(
    () =>
      value ? [{ uid: 'current', name: t('master.f.image'), status: 'done', url: value }] : [],
    [value, t],
  );

  const change = async (next: UploadFile[]) => {
    const file = next[0]?.originFileObj;
    if (!file) {
      onChange?.(null);
      return;
    }

    setReading(true);
    try {
      onChange?.(await toImageSource(file));
    } catch {
      void message.error(t('master.f.imageFailed'));
    } finally {
      setReading(false);
    }
  };

  return (
    <Spin spinning={reading}>
      <div className="erp-master-image">
        <ImageDrop
          value={files}
          disabled={disabled}
          hint={t('master.f.imageHint')}
          onChange={(next) => void change(next)}
        />
      </div>
    </Spin>
  );
}
