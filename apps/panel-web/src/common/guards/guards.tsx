import type { ReactNode } from 'react';
import { Navigate, useLocation } from 'react-router';
import { CHANGE_PASSWORD_PATH, LOGIN_PATH } from '@/common/constants/routes';
import { isSignedIn, useAuthStore } from '@/common/store/auth-store';
import { redirectTarget } from './redirect';

export function RequireSignedIn({ children }: { children: ReactNode }) {
  const signedIn = useAuthStore(isSignedIn);
  const mustChange = useAuthStore((s) => s.mustChangePassword);
  const location = useLocation();
  const from = `${location.pathname}${location.search}`;

  if (!signedIn) return <Navigate to={LOGIN_PATH} replace state={{ from }} />;
  if (mustChange && location.pathname !== CHANGE_PASSWORD_PATH) {
    return <Navigate to={CHANGE_PASSWORD_PATH} replace state={{ from }} />;
  }
  return children;
}

export function GuestOnly({ children }: { children: ReactNode }) {
  const signedIn = useAuthStore(isSignedIn);
  const mustChange = useAuthStore((s) => s.mustChangePassword);
  const location = useLocation();
  const target = redirectTarget(location.state);

  if (!signedIn) return children;
  if (mustChange) return <Navigate to={CHANGE_PASSWORD_PATH} replace state={{ from: target }} />;
  return <Navigate to={target} replace />;
}
