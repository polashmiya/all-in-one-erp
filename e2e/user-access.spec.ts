import fs from 'node:fs';
import path from 'node:path';
import { expect, test, type APIRequestContext, type Page } from '@playwright/test';
import { E2E } from './e2e.config';

const START_PASSWORD = 'Start@12345';
const OWN_PASSWORD = 'Own@123456';
const RESET_PASSWORD = 'Reset@12345';
const VIEWER_ROLE = 'Viewer';
const CODE_PATTERN = /\b(\d{6})\b/;

const unique = () => Date.now().toString(36);

async function panelHeaders(request: APIRequestContext) {
  const login = await request.post(`${E2E.apiUrl}/api/panel/auth/login`, {
    data: { login: E2E.panelUser.email, password: E2E.panelUser.password },
  });
  const { accessToken } = (await login.json()) as { accessToken: string };
  return { Authorization: `Bearer ${accessToken}` };
}

async function demoAccountId(request: APIRequestContext, headers: Record<string, string>) {
  const search = encodeURIComponent(E2E.demoAccount);
  const accounts = await request.get(`${E2E.apiUrl}/api/panel/accounts?search=${search}`, {
    headers,
  });
  const { items } = (await accounts.json()) as { items: { id: string }[] };
  return items[0]!.id;
}

async function erpHeaders(request: APIRequestContext) {
  const login = await request.post(`${E2E.apiUrl}/api/erp/auth/login`, {
    data: { login: E2E.erpUser.email, password: E2E.erpUser.password },
  });
  const body = (await login.json()) as {
    accessToken: string;
    businessUnits: { id: string; name: string }[];
  };
  const unit = body.businessUnits.find((u) => u.name === E2E.businessUnit)!;
  return {
    Authorization: `Bearer ${body.accessToken}`,
    'X-Business-Unit': unit.id,
    unitId: unit.id,
  };
}

async function createErpUser(request: APIRequestContext, username: string) {
  const { unitId, ...headers } = await erpHeaders(request);
  const response = await request.post(`${E2E.apiUrl}/api/erp/user-management/users`, {
    headers,
    data: {
      fullName: `User ${username}`,
      username,
      email: `${username}@demopharma.com`,
      phone: null,
      employeeId: null,
      password: START_PASSWORD,
      businessUnitIds: [unitId],
    },
  });
  expect(response.ok()).toBeTruthy();
  const { user } = (await response.json()) as { user: { id: string } };
  const roles = await request.get(`${E2E.apiUrl}/api/erp/user-management/roles/options`, {
    headers,
  });
  const viewer = ((await roles.json()) as { id: string; name: string }[]).find(
    (r) => r.name === VIEWER_ROLE,
  )!;
  const assigned = await request.put(
    `${E2E.apiUrl}/api/erp/user-management/users/${user.id}/role`,
    {
      headers,
      data: { roleId: viewer.id },
    },
  );
  expect(assigned.ok()).toBeTruthy();
}

async function signIn(page: Page, login: string, password: string) {
  await page.goto('/login');
  await page.getByLabel('Email or username').fill(login);
  await page.getByLabel('Password').fill(password);
  await page.getByRole('button', { name: /Sign in/ }).click();
}

async function changeForcedPassword(page: Page, current: string) {
  await expect(page.getByRole('heading', { name: 'Change your password' })).toBeVisible();
  await page.getByLabel('Current password').fill(current);
  await page.locator('input[type=password]').nth(1).fill(OWN_PASSWORD);
  await page.locator('input[type=password]').nth(2).fill(OWN_PASSWORD);
  await page.getByRole('button', { name: 'Change password' }).click();
}

function latestCode(address: string) {
  const files = fs
    .readdirSync(E2E.mailFolder)
    .map((name) => path.join(E2E.mailFolder, name))
    .sort((a, b) => fs.statSync(b).mtimeMs - fs.statSync(a).mtimeMs);
  const mail = files
    .map((file) => fs.readFileSync(file, 'utf8'))
    .find((text) => text.includes(address));
  const body = mail
    ?.split(/\r?\n\r?\n/)
    .slice(1)
    .join('\n');
  const text = body
    ?.replace(/=\r?\n/g, '')
    .replace(/=([0-9A-F]{2})/g, (_, hex: string) => String.fromCharCode(parseInt(hex, 16)));
  return text?.match(CODE_PATTERN)?.[1];
}

test('a Panel admin builds a role and a user who signs in by username and sees only that screen', async ({
  browser,
  request,
}) => {
  const id = unique();
  const panel = await panelHeaders(request);
  const accountId = await demoAccountId(request, panel);
  const admin = { username: `adm.${id}`, email: `adm.${id}@demopharma.com` };
  const added = await request.post(`${E2E.apiUrl}/api/panel/accounts/${accountId}/admins`, {
    headers: panel,
    data: { fullName: `Admin ${id}`, ...admin, phone: null, password: START_PASSWORD },
  });
  expect(added.ok()).toBeTruthy();

  const page = await browser.newPage({ baseURL: E2E.erpUrl });
  await signIn(page, admin.username, START_PASSWORD);
  await changeForcedPassword(page, START_PASSWORD);
  await page.getByRole('button', { name: /Continue to/ }).click();
  await expect(page).toHaveURL(/dashboard/);

  const roleName = `Viewer ${id}`;
  await page.goto('/app/system-setup/user-role/new');
  await page.getByLabel('Role name').fill(roleName);
  await page.getByRole('checkbox', { name: 'View · Item Profile' }).check();
  await page.getByRole('button', { name: /^(save )?Save$/ }).click();
  await expect(page).toHaveURL(/user-role$/);

  const username = `usr.${id}`;
  await page.goto('/app/system-setup/user/new');
  await page.getByLabel('Full name').fill(`User ${id}`);
  await page.getByLabel('Username').fill(username);
  await page.getByLabel('Email').fill(`${username}@demopharma.com`);
  await page.locator('input[type=password]').nth(0).fill(START_PASSWORD);
  await page.locator('input[type=password]').nth(1).fill(START_PASSWORD);
  await page.getByRole('button', { name: /Save user/ }).click();
  await expect(page).toHaveURL(/system-setup\/user$/);

  const row = page.getByRole('row', { name: new RegExp(username) });
  await row.getByRole('button', { name: 'Assign' }).click();
  await page.getByRole('dialog').getByRole('combobox').click();
  await page.keyboard.type(roleName);
  await page.locator(`.ant-select-item-option[title="${roleName}"]`).click();
  await page.getByRole('button', { name: 'Save role' }).click();
  await expect(row.getByText(roleName)).toBeVisible();

  const user = await browser.newPage({ baseURL: E2E.erpUrl });
  await signIn(user, username, START_PASSWORD);
  await changeForcedPassword(user, START_PASSWORD);
  await user.getByRole('button', { name: /Continue to/ }).click();
  await expect(user).toHaveURL(/dashboard/);
  await user.getByText('System Setup', { exact: true }).click();
  await expect(user.getByText('Item Management', { exact: true })).toBeVisible();
  await expect(user.getByText('User Management', { exact: true })).toHaveCount(0);
});

test('a forgotten password is reset with the emailed code', async ({ browser, request }) => {
  const username = `fgt.${unique()}`;
  await createErpUser(request, username);

  const page = await browser.newPage({ baseURL: E2E.erpUrl });
  await page.goto('/login');
  await page
    .getByRole('link', { name: 'Forgot password?' })
    .or(page.getByRole('button', { name: 'Forgot password?' }))
    .click();
  await expect(page.getByRole('heading', { name: 'Forgot password' })).toBeVisible();
  await page.getByLabel('Email or username').fill(username);
  await page.getByRole('button', { name: 'Send code' }).click();
  await expect(page.getByText('Check your email')).toBeVisible();

  await expect.poll(() => latestCode(`${username}@demopharma.com`)).toBeTruthy();
  const code = latestCode(`${username}@demopharma.com`)!;
  const wrongCode = code.replace(/^\d/, (digit) => String((Number(digit) + 1) % 10));
  await page.locator('.ant-otp input').first().click();
  await page.keyboard.type(wrongCode);
  await page.locator('input[type=password]').nth(0).fill(RESET_PASSWORD);
  await page.locator('input[type=password]').nth(1).fill(RESET_PASSWORD);
  await page.getByRole('button', { name: 'Reset password' }).click();
  await expect(page.getByText('The code is wrong or has expired.')).toBeVisible();

  await page.locator('.ant-otp input').first().click();
  await page.keyboard.type(code);
  await page.getByRole('button', { name: 'Reset password' }).click();
  await expect(page.getByText('Password changed')).toBeVisible();

  const login = await request.post(`${E2E.apiUrl}/api/erp/auth/login`, {
    data: { login: username, password: RESET_PASSWORD },
  });
  expect(login.ok()).toBeTruthy();
});

test('three wrong passwords lock the user until a Panel SuperAdmin unlocks it', async ({
  request,
}) => {
  const username = `lck.${unique()}`;
  await createErpUser(request, username);
  const tryLogin = (password: string) =>
    request.post(`${E2E.apiUrl}/api/erp/auth/login`, { data: { login: username, password } });

  for (let attempt = 0; attempt < 3; attempt++)
    expect((await tryLogin('Wrong@12345')).ok()).toBeFalsy();
  expect((await tryLogin(START_PASSWORD)).ok()).toBeFalsy();

  const panel = await panelHeaders(request);
  const found = await request.get(`${E2E.apiUrl}/api/panel/erp-users?search=${username}`, {
    headers: panel,
  });
  const { items } = (await found.json()) as { items: { id: string; isLocked: boolean }[] };
  expect(items[0]!.isLocked).toBeTruthy();
  const unlock = await request.post(`${E2E.apiUrl}/api/panel/erp-users/${items[0]!.id}/unlock`, {
    headers: panel,
  });
  expect(unlock.ok()).toBeTruthy();

  expect((await tryLogin(START_PASSWORD)).ok()).toBeTruthy();
});
