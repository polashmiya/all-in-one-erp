import { configureApiClient, panelLogout, panelRefresh } from '@erp/api-client';
import { QueryClient } from '@tanstack/react-query';
import { appConfig } from '@/common/config/app.config';
import { useAuthStore } from '@/common/store/auth-store';

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: { staleTime: appConfig.queryStaleMs, retry: 1, refetchOnWindowFocus: false },
  },
});

const clearSession = () => {
  useAuthStore.getState().signOut();
  queryClient.clear();
};

configureApiClient({
  baseUrl: import.meta.env.VITE_API_BASE_URL,
  getAccessToken: () => useAuthStore.getState().accessToken,
  refreshSession: async () => {
    const { refreshToken, setTokens } = useAuthStore.getState();
    if (!refreshToken) return false;
    try {
      setTokens(await panelRefresh({ refreshToken }));
      return true;
    } catch {
      return false;
    }
  },
  onUnauthorized: () => {
    if (useAuthStore.getState().refreshToken) clearSession();
  },
});

export function endSession() {
  void panelLogout().catch(() => undefined);
  clearSession();
}
