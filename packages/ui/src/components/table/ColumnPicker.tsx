import Icon, { SearchOutlined } from '@ant-design/icons';
import { Button, Checkbox, Input, Popover, Tooltip } from 'antd';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

const ColumnsSvg = () => (
  <svg
    viewBox="0 0 16 16"
    width="1em"
    height="1em"
    fill="none"
    stroke="currentColor"
    strokeWidth="1.4"
  >
    <rect x="1.7" y="1.7" width="12.6" height="12.6" rx="2" />
    <path d="M6.1 1.7v12.6M9.9 1.7v12.6" />
  </svg>
);

export interface PickerColumn {
  key: string;
  title: string;
}

export function ColumnPicker({
  columns,
  hidden,
  onChange,
}: {
  columns: PickerColumn[];
  hidden: string[];
  onChange: (hidden: string[]) => void;
}) {
  const { t } = useTranslation();
  const [query, setQuery] = useState('');
  const term = query.trim().toLowerCase();
  const shown = columns.filter((c) => c.title.toLowerCase().includes(term));
  const visibleCount = columns.filter((c) => !hidden.includes(c.key)).length;

  const toggle = (key: string, visible: boolean) =>
    onChange(visible ? hidden.filter((k) => k !== key) : [...hidden, key]);

  return (
    <Popover
      trigger="click"
      placement="bottomRight"
      arrow={false}
      onOpenChange={(open) => !open && setQuery('')}
      content={
        <div className="erp-colpicker">
          <Input
            allowClear
            prefix={<SearchOutlined />}
            placeholder={t('table.searchColumns')}
            value={query}
            onChange={(e) => setQuery(e.target.value)}
          />
          <div className="erp-colpicker__list">
            {shown.map((c) => (
              <Checkbox
                key={c.key}
                checked={!hidden.includes(c.key)}
                onChange={(e) => toggle(c.key, e.target.checked)}
              >
                {c.title}
              </Checkbox>
            ))}
            {!shown.length && <span className="erp-colpicker__empty">{t('field.noMatches')}</span>}
          </div>
          <div className="erp-colpicker__foot">
            <span>{t('table.columnsShown', { shown: visibleCount, total: columns.length })}</span>
            {hidden.length > 0 && (
              <button type="button" className="erp-linkbtn" onClick={() => onChange([])}>
                {t('common.reset')}
              </button>
            )}
          </div>
        </div>
      }
    >
      <Tooltip title={t('table.columns')}>
        <Button
          type="text"
          size="small"
          icon={<Icon component={ColumnsSvg} />}
          aria-label={t('table.columns')}
        />
      </Tooltip>
    </Popover>
  );
}
