import { expect, test, type Page } from '@playwright/test';
import { E2E } from './e2e.config';

const OUT = process.env.SHOT_DIR!;

async function signIn(page: Page, user: { email: string; password: string }) {
  await page.goto('/login');
  await page.getByLabel('Email').fill(user.email);
  await page.getByLabel('Password').fill(user.password);
  await page
    .getByRole('button')
    .filter({ hasText: /^\s*Sign in\s*$/ })
    .click();
  await expect(page).not.toHaveURL(/login/);
}

test('erp', async ({ browser }) => {
  const page = await browser.newPage({
    baseURL: E2E.erpUrl,
    viewport: { width: 1440, height: 900 },
  });
  const errors: string[] = [];
  page.on('console', (m) => m.type() === 'error' && errors.push(m.text()));
  await page.goto('/login');
  await page.waitForTimeout(800);
  await page.screenshot({ path: `${OUT}/login.png` });
  await signIn(page, E2E.erpUser);
  await page.getByRole('button', { name: /Continue to/ }).click();
  await page.waitForTimeout(1500);
  const snap = (name: string) => page.screenshot({ path: `${OUT}/${name}.png` });
  const open = async (path: string) => {
    await page.goto(path);
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(700);
  };
  await open('/app/procurement/purchase-order/new');
  await page
    .getByRole('button')
    .filter({ hasText: /Save purchase order/i })
    .first()
    .click();
  await page.waitForTimeout(900);
  await snap('errors');
  await page.mouse.wheel(0, 1500);
  await page.waitForTimeout(500);
  await snap('lines');
  await open('/app/system-setup/item-profile/new');
  await page.getByRole('combobox', { name: 'Item type' }).click();
  await page.waitForTimeout(600);
  await snap('dropdown');
  await open('/app/accounting/journal/new');
  await snap('journal');
  await open('/app/procurement/purchase-report');
  await snap('report');
  await open('/app/accounting/trial-balance');
  await snap('trial');
  await open('/app/system-setup/item-profile');
  await page.locator('.erp-filter').first().click();
  await page.waitForTimeout(500);
  await snap('filter-open');
  await page.locator('.erp-filterpop__item').nth(1).click();
  await page.waitForTimeout(600);
  await page.getByRole('textbox').first().fill('ink');
  await page.waitForTimeout(800);
  await snap('filter-set');
  await open('/app/system-setup/user-role');
  await page.locator('tbody tr.ant-table-row').first().click();
  await page.waitForTimeout(1200);
  await snap('role');
  await open('/app/system-setup/user/new');
  await snap('user-form');
  await page.evaluate(() => {
    const key = 'erp.ui.preferences';
    const saved = JSON.parse(localStorage.getItem(key)!);
    saved.state.mode = 'dark';
    saved.state.font = 'inter';
    saved.version = 2;
    localStorage.setItem(key, JSON.stringify(saved));
  });
  await open('/app/procurement/purchase-order/new');
  await snap('dark');
  console.log(
    'migrated',
    await page.evaluate(
      () => JSON.parse(localStorage.getItem('erp.ui.preferences')!).state.font,
    ),
    await page.evaluate(() => {
      const c = document.createElement('span');
      c.style.cssText = 'font:13px "Onest Variable";font-variant-numeric:tabular-nums;position:absolute';
      document.body.append(c);
      const w = (t: string) => ((c.textContent = t), c.getBoundingClientRect().width);
      return [w('1111'), w('8888'), w('0000')];
    }),
  );
  await page.evaluate(() => {
    const key = 'erp.ui.preferences';
    const saved = JSON.parse(localStorage.getItem(key)!);
    saved.state.mode = 'light';
    localStorage.setItem(key, JSON.stringify(saved));
  });
  await page.reload();
  await page.getByRole('button', { name: /Demo Admin/ }).click();
  await page.getByText('বাংলা').first().click();
  await page.keyboard.press('Escape');
  await open('/app/procurement/purchase-order/new');
  await snap('bangla');
  await page.setViewportSize({ width: 390, height: 844 });
  await page.waitForTimeout(700);
  await snap('mobile');
  console.log('errors', errors);
});

test('panel', async ({ browser }) => {
  const page = await browser.newPage({
    baseURL: E2E.panelUrl,
    viewport: { width: 1440, height: 900 },
  });
  await signIn(page, E2E.panelUser);
  await page.goto('/accounts/new');
  await page.waitForLoadState('networkidle');
  await page.waitForTimeout(700);
  await page.screenshot({ path: `${OUT}/p-setup.png` });
  await page.goto('/feature-permissions');
  await page.getByRole('combobox').first().click();
  await page.locator('.ant-select-item-option').first().click();
  await page.waitForTimeout(1500);
  await page.screenshot({ path: `${OUT}/p-tree.png` });
});
