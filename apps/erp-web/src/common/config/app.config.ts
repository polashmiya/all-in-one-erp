const env = import.meta.env;

export const appConfig = {
  name: env.VITE_APP_NAME ?? 'myErp',
  demoAccount:
    env.DEV && env.VITE_DEMO_EMAIL && env.VITE_DEMO_PASSWORD
      ? { login: env.VITE_DEMO_EMAIL, password: env.VITE_DEMO_PASSWORD }
      : undefined,
  defaultPageSize: 25,
  queryStaleMs: 30_000,
  sessionStaleMs: 5 * 60_000,
  referenceStaleMs: 60 * 60_000,
};
