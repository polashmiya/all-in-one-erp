import {
  CHANGE_PASSWORD_PATH,
  FORGOT_PASSWORD_PATH,
  HOME_PATH,
  LOGIN_PATH,
  SELECT_UNIT_PATH,
} from '@/common/constants/routes';

const DEFAULT_TARGET = HOME_PATH;
const AUTH_PATHS = [LOGIN_PATH, SELECT_UNIT_PATH, CHANGE_PASSWORD_PATH, FORGOT_PASSWORD_PATH];

export function redirectTarget(state: unknown): string {
  const from = (state as { from?: unknown } | null)?.from;
  if (typeof from !== 'string' || !from.startsWith('/') || from.startsWith('//'))
    return DEFAULT_TARGET;
  if (AUTH_PATHS.some((path) => from === path || from.startsWith(`${path}?`)))
    return DEFAULT_TARGET;
  return from;
}
