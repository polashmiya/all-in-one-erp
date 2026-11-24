import { useMemo } from 'react';
import { useAuthStore } from '@/common/store/auth-store';

export interface FeatureAccess {
  view: boolean;
  create: boolean;
  edit: boolean;
  delete: boolean;
  export: boolean;
  approve: boolean;
  cancel: boolean;
}

export function useFeatureAccess(feature: string): FeatureAccess {
  const permissions = useAuthStore((s) => s.permissions);

  return useMemo(() => {
    const granted = new Set(permissions);
    const has = (action: string) => granted.has(`${feature}.${action}`);
    return {
      view: has('view'),
      create: has('create'),
      edit: has('edit'),
      delete: has('delete'),
      export: has('export'),
      approve: has('approve'),
      cancel: has('cancel'),
    };
  }, [permissions, feature]);
}
