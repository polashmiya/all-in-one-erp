import { DatePicker } from 'antd';
import dayjs from 'dayjs';
import { API_DATE_FORMAT, DATE_FORMAT } from '../../utils/format';

export interface DateFilterProps {
  label: string;
  value?: string | null;
  onChange: (value: string | null) => void;
}

export function DateFilter({ label, value, onChange }: DateFilterProps) {
  return (
    <DatePicker
      className="erp-w-full"
      aria-label={label}
      placeholder={label}
      prefix={value ? <span className="erp-filter__label">{label}:</span> : undefined}
      value={value ? dayjs(value) : null}
      onChange={(date) => onChange(date ? date.format(API_DATE_FORMAT) : null)}
      format={DATE_FORMAT}
    />
  );
}
