import {
  CHANGE_PASSWORD_PATH,
  FORGOT_PASSWORD_PATH,
  HOME_PATH,
  LOGIN_PATH,
} from '@/common/constants/routes';

const AUTH_PATHS = [LOGIN_PATH, CHANGE_PASSWORD_PATH, FORGOT_PASSWORD_PATH];

export function redirectTarget(state: unknown): string {
  const from = (state as { from?: unknown } | null)?.from;
  if (typeof from !== 'string' || !from.startsWith('/') || from.startsWith('//')) return HOME_PATH;
  if (AUTH_PATHS.some((path) => from.startsWith(path))) return HOME_PATH;
  return from;
}
