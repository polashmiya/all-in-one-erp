import { defineConfig } from '@playwright/test';
import { E2E } from './e2e.config';

const origins = { Cors__AllowedOrigins__0: E2E.panelUrl, Cors__AllowedOrigins__1: E2E.erpUrl };

export default defineConfig({
  testDir: '.',
  timeout: 120_000,
  expect: { timeout: 15_000 },
  workers: 1,
  reporter: [['list'], ['html', { open: 'never', outputFolder: 'playwright-report' }]],
  outputDir: 'test-results',
  use: {
    channel: 'chrome',
    headless: true,
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
  },
  webServer: [
    {
      command: `dotnet run --project backend/src/Erp.Api --no-launch-profile --artifacts-path e2e/.build`,
      cwd: '..',
      url: `${E2E.apiUrl}/health`,
      timeout: 300_000,
      reuseExistingServer: false,
      env: {
        ASPNETCORE_ENVIRONMENT: 'Development',
        ASPNETCORE_URLS: E2E.apiUrl,
        ConnectionStrings__Erp: E2E.connectionString,
        ...origins,
      },
    },
    {
      command: `pnpm --filter @erp/panel-web exec vite --port ${E2E.panelPort} --strictPort`,
      cwd: '..',
      url: E2E.panelUrl,
      timeout: 120_000,
      reuseExistingServer: false,
      env: { VITE_API_BASE_URL: E2E.apiUrl },
    },
    {
      command: `pnpm --filter @erp/erp-web exec vite --port ${E2E.erpPort} --strictPort`,
      cwd: '..',
      url: E2E.erpUrl,
      timeout: 120_000,
      reuseExistingServer: false,
      env: { VITE_API_BASE_URL: E2E.apiUrl },
    },
  ],
});
