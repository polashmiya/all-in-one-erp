import { expect, test, type APIRequestContext, type Locator, type Page } from '@playwright/test';
import { E2E } from './e2e.config';

const DEPARTMENT_FEATURE = 'system-setup.item-department';
const DEPARTMENT_PATH = '/app/system-setup/item-department';

const unique = () => Date.now().toString(36).toUpperCase();

const button = (scope: Page | Locator, text: string) =>
  scope.getByRole('button').filter({ hasText: new RegExp(`^\\s*${text}\\s*$`) });

async function signIn(page: Page, user: { email: string; password: string }) {
  await page.goto('/login');
  await page.getByLabel('Email').fill(user.email);
  await page.getByLabel('Password').fill(user.password);
  await button(page, 'Sign in').click();
}

async function choose(scope: Locator, label: string, option: string) {
  await scope.getByRole('combobox', { name: label }).click();
  await scope.page().locator(`.ant-select-item-option[title="${option}"]`).click();
}

async function demoAccountId(request: APIRequestContext) {
  const login = await request.post(`${E2E.apiUrl}/api/panel/auth/login`, { data: E2E.panelUser });
  const { accessToken } = (await login.json()) as { accessToken: string };
  const search = encodeURIComponent(E2E.demoAccount);
  const accounts = await request.get(`${E2E.apiUrl}/api/panel/accounts?search=${search}`, {
    headers: { Authorization: `Bearer ${accessToken}` },
  });
  const { items } = (await accounts.json()) as { items: { id: string }[] };
  return items[0]!.id;
}

test('a custom field added in the Panel is shown, saved and kept in the ERP', async ({
  browser,
  request,
}) => {
  const id = unique();
  const fieldLabel = `E2E note ${id}`;
  const departmentName = `E2E department ${id}`;
  const accountId = await demoAccountId(request);

  const panel = await browser.newPage({ baseURL: E2E.panelUrl });
  await signIn(panel, E2E.panelUser);
  await expect(panel).not.toHaveURL(/login/);

  await panel.goto(`/accounts/${accountId}/configuration?feature=${DEPARTMENT_FEATURE}`);
  await button(panel, 'New custom field').click();
  const dialog = panel.getByRole('dialog');
  await dialog.getByLabel('Label (English)').fill(fieldLabel);
  await choose(dialog, 'Type', 'Text');
  await choose(dialog, 'Section', 'General › Details');
  await button(dialog, 'Save').click();
  await expect(panel.getByText('Custom field added')).toBeVisible();
  await expect(panel.getByText(fieldLabel)).toBeVisible();

  await panel.goto(`/accounts/${accountId}/history`);
  await expect(panel.getByText('Custom field').first()).toBeVisible();

  const erp = await browser.newPage({ baseURL: E2E.erpUrl });
  await signIn(erp, E2E.erpUser);
  await erp.getByText(E2E.businessUnit, { exact: true }).click();
  await button(erp, `Continue to ${E2E.businessUnit}`).click();
  await expect(erp).not.toHaveURL(/select-unit|login/);

  await erp.goto(DEPARTMENT_PATH);
  await button(erp, 'New department').click();
  const modal = erp.getByRole('dialog');
  await modal.getByLabel('Department code').fill(`E${id}`.slice(0, 20));
  await modal.getByLabel('Department name').fill(departmentName);
  await modal.getByLabel(fieldLabel).fill('Kept by E2E');
  await button(modal, 'Create').click();
  await expect(erp.getByText(`“${departmentName}” created`)).toBeVisible();

  await erp.getByText(departmentName, { exact: true }).click();
  await expect(erp.getByRole('dialog').getByLabel(fieldLabel)).toHaveValue('Kept by E2E');
});
