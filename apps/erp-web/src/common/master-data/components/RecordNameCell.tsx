import { InitialsTile, TitleCell } from '@erp/ui';
import type { ReactNode } from 'react';
import '../master-data.css';

export function RecordNameCell({
  name,
  image,
  index,
  subtitle,
  showImage = true,
  onOpen,
}: {
  name: string;
  image?: string | null;
  index: number;
  subtitle?: ReactNode;
  showImage?: boolean;
  onOpen?: () => void;
}) {
  const leading = !showImage ? undefined : image ? (
    <img src={image} alt="" className="erp-initials erp-record-avatar" />
  ) : (
    <InitialsTile name={name} index={index} />
  );

  return (
    <TitleCell
      leading={leading}
      title={
        <button
          type="button"
          className="erp-master-name"
          disabled={!onOpen}
          onClick={(event) => {
            event.stopPropagation();
            onOpen?.();
          }}
        >
          {name}
        </button>
      }
      subtitle={subtitle}
    />
  );
}
