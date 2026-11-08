import { DeleteOutlined, PaperClipOutlined } from '@ant-design/icons';
import { App, Button, Upload } from 'antd';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toImageSource } from '../../common/image';
import '../item-form.css';

const IMAGE_ACCEPT = 'image/png,image/jpeg,image/webp,image/gif';

export function ImageAttach({
  id,
  value,
  onChange,
}: {
  id?: string;
  value?: string | null;
  onChange?: (value: string | null) => void;
}) {
  const { t } = useTranslation();
  const { message } = App.useApp();
  const [reading, setReading] = useState(false);

  const attach = async (file: File) => {
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
    <div className="erp-item-attach">
      {value && <img src={value} alt="" className="erp-item-attach__thumb" />}
      <Upload
        accept={IMAGE_ACCEPT}
        showUploadList={false}
        beforeUpload={(file) => {
          void attach(file);
          return false;
        }}
      >
        <Button id={id} icon={<PaperClipOutlined />} loading={reading}>
          {value ? t('item.f.changeImage') : t('item.f.attachImage')}
        </Button>
      </Upload>
      {value && (
        <Button
          type="text"
          danger
          icon={<DeleteOutlined />}
          aria-label={t('item.f.removeImage')}
          onClick={() => onChange?.(null)}
        />
      )}
    </div>
  );
}
