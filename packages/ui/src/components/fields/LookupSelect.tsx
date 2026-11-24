import { PlusOutlined } from '@ant-design/icons';
import { Select, Spin, Typography, type SelectProps } from 'antd';
import { useEffect, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';

export interface LookupOption {
  value: string;
  label: string;
  description?: string;
}

export interface LookupSelectProps extends Omit<
  SelectProps<string>,
  'options' | 'onSearch' | 'filterOption' | 'showSearch'
> {
  fetchOptions: (query: string) => Promise<LookupOption[]>;
  onCreate?: (query: string) => void;
  debounceMs?: number;
}

export function LookupSelect({
  fetchOptions,
  onCreate,
  debounceMs = 250,
  placeholder,
  ...props
}: LookupSelectProps) {
  const { t } = useTranslation();
  const [query, setQuery] = useState('');
  const [options, setOptions] = useState<LookupOption[]>([]);
  const [loading, setLoading] = useState(false);
  const request = useRef(0);
  const fetchRef = useRef(fetchOptions);
  fetchRef.current = fetchOptions;

  useEffect(() => {
    const id = ++request.current;
    setLoading(true);
    const timer = setTimeout(() => {
      void fetchRef
        .current(query)
        .then((result) => id === request.current && setOptions(result))
        .finally(() => id === request.current && setLoading(false));
    }, debounceMs);
    return () => clearTimeout(timer);
  }, [query, debounceMs]);

  return (
    <Select<string>
      showSearch
      allowClear
      filterOption={false}
      onSearch={setQuery}
      placeholder={placeholder ?? t('field.searchPlaceholder')}
      notFoundContent={loading ? <Spin size="small" /> : t('field.noMatches')}
      optionRender={(option) => (
        <span className="erp-option">
          <span>{option.data.label}</span>
          {option.data.description && (
            <Typography.Text type="secondary" className="erp-text-sm">
              {option.data.description}
            </Typography.Text>
          )}
        </span>
      )}
      popupRender={(menu) => (
        <>
          {menu}
          {onCreate && query.trim() && (
            <button
              type="button"
              className="erp-menu-item erp-menu-item--create"
              onClick={() => onCreate(query.trim())}
            >
              <PlusOutlined />
              {t('field.create', { value: query.trim() })}
            </button>
          )}
        </>
      )}
      options={options}
      {...props}
    />
  );
}
