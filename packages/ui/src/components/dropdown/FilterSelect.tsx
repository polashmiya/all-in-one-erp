import { CheckOutlined, DownOutlined, SearchOutlined } from '@ant-design/icons';
import { Input, Popover } from 'antd';
import { useState, type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';

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

const textOf = (option: FilterOption) =>
  typeof option.label === 'string' ? option.label : option.value;

export function FilterSelect({
  label,
  options,
  value,
  onChange,
  width,
  allLabel,
}: FilterSelectProps) {
  const { t } = useTranslation();
  const [open, setOpen] = useState(false);
  const [query, setQuery] = useState('');
  const selected = options.find((o) => o.value === value);
  const term = query.trim().toLowerCase();
  const matches = options.filter((o) => textOf(o).toLowerCase().includes(term));

  const toggle = (next: boolean) => {
    setOpen(next);
    if (!next) setQuery('');
  };

  const pick = (next: string | null) => {
    onChange(next);
    toggle(false);
  };

  const item = (key: string, text: ReactNode, active: boolean, onClick: () => void) => (
    <button
      key={key}
      type="button"
      role="option"
      aria-selected={active}
      className={`erp-filterpop__item${active ? ' is-active' : ''}`}
      onClick={onClick}
    >
      <span className="erp-filterpop__text">{text}</span>
      {active && <CheckOutlined />}
    </button>
  );

  return (
    <Popover
      open={open}
      onOpenChange={toggle}
      trigger="click"
      placement="bottomLeft"
      arrow={false}
      destroyOnHidden
      content={
        <div className="erp-filterpop">
          <Input
            autoFocus
            allowClear
            prefix={<SearchOutlined />}
            placeholder={t('field.searchPlaceholder')}
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            onPressEnter={() => matches[0] && pick(matches[0].value)}
            onKeyDown={(e) => e.key === 'Escape' && toggle(false)}
          />
          <div className="erp-filterpop__list" role="listbox" aria-label={label}>
            {!term && item('__all', allLabel ?? t('filter.all'), !value, () => pick(null))}
            {matches.map((o) => item(o.value, o.label, o.value === value, () => pick(o.value)))}
            {!matches.length && (
              <span className="erp-filterpop__empty">{t('field.noMatches')}</span>
            )}
          </div>
        </div>
      }
    >
      <button
        type="button"
        className={`erp-filter${selected ? ' is-set' : ''}`}
        style={{ width }}
        aria-label={label}
        aria-expanded={open}
      >
        <span className="erp-filter__text">
          {selected ? (
            <>
              <span className="erp-filter__label">{label}:</span>
              {selected.label}
            </>
          ) : (
            <span className="erp-filter__placeholder">{label}</span>
          )}
        </span>
        <DownOutlined className="erp-filter__chev" />
      </button>
    </Popover>
  );
}
