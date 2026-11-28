import { Select } from 'antd';
import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { sizes } from '../../theme/design-tokens';

export interface FilterOption {
  value: string;
  label: ReactNode;
}

export interface FilterSelectProps {
  label: string;
  options: FilterOption[];
  value?: string | null;
  onChange: (value: string | null) => void;
  width?: number;
  allLabel?: ReactNode;
}

const ALL = '__all';

const textOf = (label: ReactNode, fallback: string) =>
  typeof label === 'string' ? label : fallback;

export function FilterSelect({
  label,
  options,
  value,
  onChange,
  width,
  allLabel,
}: FilterSelectProps) {
  const { t } = useTranslation();
  const selected = options.some((o) => o.value === value);
  const all = allLabel ?? t('filter.all');

  return (
    <Select<string>
      showSearch
      className={`erp-filter${selected ? ' is-set' : ''}`}
      style={{ width }}
      aria-label={label}
      value={selected ? value! : ALL}
      prefix={selected ? <span className="erp-filter__label">{label}:</span> : undefined}
      labelRender={(item) =>
        item.value === ALL ? <span className="erp-filter__placeholder">{label}</span> : item.label
      }
      options={[
        { value: ALL, label: all, search: textOf(all, '') },
        ...options.map((o) => ({
          value: o.value,
          label: o.label,
          search: textOf(o.label, o.value),
        })),
      ]}
      optionFilterProp="search"
      popupMatchSelectWidth={sizes.filterMaxWidth}
      notFoundContent={t('field.noMatches')}
      onChange={(next) => onChange(next === ALL ? null : next)}
    />
  );
}
