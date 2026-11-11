import {
  getGetItemOptionsQueryKey,
  useGetItemOptions,
  type ItemDetail,
  type ItemLookup,
} from '@erp/api-client';
import { useDebouncedValue } from '@erp/ui';
import { keepPreviousData } from '@tanstack/react-query';
import { Select } from 'antd';
import { useState } from 'react';
import { lookupLabel, useUnitKey } from '@/common/master-data';

const masterOf = (source?: ItemDetail): ItemLookup | undefined =>
  source?.masterItemId
    ? {
        id: source.masterItemId,
        code: source.masterItemCode,
        name: source.masterItemName ?? '',
        isActive: true,
        parentId: null,
      }
    : undefined;

export function MasterItemSelect({
  id,
  source,
  excludeId,
  placeholder,
  value,
  onChange,
}: {
  id?: string;
  source?: ItemDetail;
  excludeId?: number;
  placeholder?: string;
  value?: number | null;
  onChange?: (value: number | null) => void;
}) {
  const [search, setSearch] = useState('');
  const [picked, setPicked] = useState(() => masterOf(source));
  const params = { search: useDebouncedValue(search).trim() || undefined };
  const query = useGetItemOptions(params, {
    query: {
      queryKey: useUnitKey(getGetItemOptionsQueryKey(params)),
      placeholderData: keepPreviousData,
    },
  });

  const found = (query.data ?? []).filter((option) => option.id !== excludeId);
  const all = picked && !found.some((o) => o.id === picked.id) ? [picked, ...found] : found;

  return (
    <Select<number>
      id={id}
      showSearch
      allowClear
      filterOption={false}
      loading={query.isFetching}
      placeholder={placeholder}
      value={value ?? undefined}
      onSearch={setSearch}
      onChange={(next) => {
        setPicked(all.find((o) => o.id === next));
        onChange?.(next ?? null);
      }}
      options={all.map((o) => ({ value: o.id, label: lookupLabel(o) }))}
    />
  );
}
