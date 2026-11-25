import {
  getGetDocumentItemOptionsQueryKey,
  useGetDocumentItemOptions,
  type DocumentItemOption,
} from '@erp/api-client';
import { useDebouncedValue } from '@erp/ui';
import { keepPreviousData } from '@tanstack/react-query';
import { Select } from 'antd';
import { useState } from 'react';
import { lookupLabel, useUnitKey } from '@/common/master-data';

export interface PickedItem {
  id: number;
  code?: string;
  name?: string;
}

export function ItemSelect({
  id,
  picked,
  placeholder,
  saleable,
  value,
  onChange,
  onPick,
}: {
  id?: string;
  picked?: PickedItem;
  placeholder?: string;
  saleable?: boolean;
  value?: number | null;
  onChange?: (value: number | null) => void;
  onPick: (option: DocumentItemOption) => void;
}) {
  const [search, setSearch] = useState('');
  const params = { search: useDebouncedValue(search).trim() || undefined, saleable };
  const query = useGetDocumentItemOptions(params, {
    query: {
      queryKey: useUnitKey(getGetDocumentItemOptionsQueryKey(params)),
      placeholderData: keepPreviousData,
    },
  });

  const found = query.data ?? [];
  const options = found.map((o) => ({ value: o.id, label: lookupLabel(o) }));
  if (picked?.name && !found.some((o) => o.id === picked.id)) {
    options.unshift({
      value: picked.id,
      label: lookupLabel({ ...picked, name: picked.name, isActive: true }),
    });
  }

  return (
    <Select<number>
      id={id}
      showSearch
      filterOption={false}
      loading={query.isFetching}
      placeholder={placeholder}
      value={value ?? undefined}
      onSearch={setSearch}
      onChange={(next) => {
        const option = found.find((o) => o.id === next);
        if (option) onPick(option);
        onChange?.(next ?? null);
      }}
      options={options}
      className="erp-w-full"
      popupMatchSelectWidth={false}
    />
  );
}
