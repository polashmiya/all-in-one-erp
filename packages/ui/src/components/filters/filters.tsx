import { SearchOutlined } from '@ant-design/icons';
import { Input, type InputRef } from 'antd';
import type { ReactNode, Ref } from 'react';

export function FilterBar({ children }: { children: ReactNode }) {
  return (
    <div className="erp-filterbar">
      <div className="erp-filterbar__main">{children}</div>
    </div>
  );
}

export interface SearchInputProps {
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  label: string;
  width?: number;
  shortcut?: string;
  ref?: Ref<InputRef>;
}

export function SearchInput({
  value,
  onChange,
  placeholder,
  label,
  width = 280,
  shortcut,
  ref,
}: SearchInputProps) {
  return (
    <Input
      ref={ref}
      allowClear
      className="erp-search"
      style={{ width }}
      prefix={<SearchOutlined />}
      suffix={
        shortcut && (
          <kbd className="erp-kbd" aria-hidden="true">
            {shortcut}
          </kbd>
        )
      }
      placeholder={placeholder}
      aria-label={label}
      value={value}
      onChange={(e) => onChange(e.target.value)}
      onKeyDown={(e) => e.key === 'Escape' && onChange('')}
    />
  );
}
