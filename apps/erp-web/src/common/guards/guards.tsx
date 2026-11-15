import type { ReactNode } from 'react';
import { Navigate, useLocation } from 'react-router';
import { CHANGE_PASSWORD_PATH, LOGIN_PATH, SELECT_UNIT_PATH } from '@/common/constants/routes';
import { isSignedIn, useAuthStore } from '@/common/store/auth-store';
import { redirectTarget } from './redirect';

function useFrom() {
  const location = useLocation();
  return `${location.pathname}${location.search}`;
}

export function RequireSignedIn({ children }: { children: ReactNode }) {
  const signedIn = useAuthStore(isSignedIn);
  const mustChange = useAuthStore((s) => s.mustChangePassword);
  const { pathname } = useLocation();
  const from = useFrom();

  if (!signedIn) return <Navigate to={LOGIN_PATH} replace state={{ from }} />;
  if (mustChange && pathname !== CHANGE_PASSWORD_PATH) {
    return <Navigate to={CHANGE_PASSWORD_PATH} replace state={{ from }} />;
  }
  return children;
}

export function RequireBusinessUnit({ children }: { children: ReactNode }) {
  const unitId = useAuthStore((s) => s.currentBusinessUnitId);
  const from = useFrom();

  return unitId ? children : <Navigate to={SELECT_UNIT_PATH} replace state={{ from }} />;
}

export function GuestOnly({ children }: { children: ReactNode }) {
  const signedIn = useAuthStore(isSignedIn);
  const mustChange = useAuthStore((s) => s.mustChangePassword);
  const hasUnit = useAuthStore((s) => s.currentBusinessUnitId !== null);
  const location = useLocation();
  const target = redirectTarget(location.state);

  if (!signedIn) return children;
  if (mustChange) return <Navigate to={CHANGE_PASSWORD_PATH} replace state={{ from: target }} />;
  return hasUnit ? (
    <Navigate to={target} replace />
  ) : (
    <Navigate to={SELECT_UNIT_PATH} replace state={{ from: target }} />
  );
}
