const env = import.meta.env;

export const appConfig = {
  name: env.VITE_APP_NAME ?? 'myErp Panel',
  demoAccount:
    env.DEV && env.VITE_DEMO_EMAIL && env.VITE_DEMO_PASSWORD
      ? { login: env.VITE_DEMO_EMAIL, password: env.VITE_DEMO_PASSWORD }
      : undefined,
  defaultPageSize: 10,
  sessionStaleMs: 5 * 60_000,
  navigationStaleMs: 10 * 60_000,
  queryStaleMs: 30_000,
};
