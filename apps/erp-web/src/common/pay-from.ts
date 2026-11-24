import { getGetBankAccountOptionsQueryKey, useGetBankAccountOptions } from '@erp/api-client';
import type { FieldBehavior, LookupItem, LookupSources } from '@erp/metadata-engine';
import { Form } from 'antd';
import { useEffect } from 'react';
import { useUnitKey } from '@/common/master-data';

const CASH_METHOD = 'CASH';
const MOBILE_METHOD = 'MFS';
const BANK_ACCOUNT_LOOKUP = 'bank-account';

export interface PayFrom {
  lookups: LookupSources;
  behaviors: Record<string, FieldBehavior>;
  isCash: (methodId: unknown) => boolean;
  useClear: () => void;
}

export function usePayFrom(methods: LookupItem[] | undefined, enabled = true): PayFrom {
  const query = useGetBankAccountOptions({
    query: { queryKey: useUnitKey(getGetBankAccountOptionsQueryKey()), enabled },
  });
  const accounts = query.data;

  const typeOf = (methodId: unknown) => {
    const code = methods?.find((m) => m.id === methodId)?.code;
    if (!code || code === CASH_METHOD) return undefined;
    return code === MOBILE_METHOD ? 'mobile' : 'bank';
  };
  const choices = (methodId: unknown) => accounts?.filter((a) => a.type === typeOf(methodId)) ?? [];

  const useClear = () => {
    const form = Form.useFormInstance();
    const methodId: unknown = Form.useWatch('paymentMethodId', form);
    const accountId: unknown = Form.useWatch('bankAccountId', form);
    const stale =
      !!accountId && !!accounts && !!methods && !choices(methodId).some((a) => a.id === accountId);
    useEffect(() => {
      if (stale) form.setFieldValue('bankAccountId', undefined);
    }, [stale, form]);
  };

  return {
    lookups: { [BANK_ACCOUNT_LOOKUP]: accounts },
    isCash: (methodId) => methods?.find((m) => m.id === methodId)?.code === CASH_METHOD,
    behaviors: {
      bankAccountId: {
        watch: ['paymentMethodId'],
        state: (values) => {
          const ids = new Set(choices(values.paymentMethodId).map((a) => a.id as number | string));
          return {
            hidden: !typeOf(values.paymentMethodId),
            required: ids.size > 0,
            filter: (item) => ids.has(item.id),
          };
        },
      },
    },
    useClear,
  };
}
