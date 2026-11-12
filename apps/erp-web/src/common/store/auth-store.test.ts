import type { BusinessUnitAccessDto, ErpLoginResponse } from '@erp/api-client';
import { isSignedIn, selectCurrentUnit, signedOut, useAuthStore } from './auth-store';

const dhaka: BusinessUnitAccessDto = { id: 'dhk', code: 'DHK', name: 'Dhaka Depot', roleName: 'Administrator' };
const ctg: BusinessUnitAccessDto = { id: 'ctg', code: 'CTG', name: 'Chattogram Depot', roleName: 'Administrator' };

const login = (overrides: Partial<ErpLoginResponse> = {}): ErpLoginResponse => ({
  accessToken: 'token',
  expiresAt: new Date(Date.now() + 3_600_000).toISOString(),
  user: {
    id: 'u1',
    email: 'admin@demopharma.com',
    fullName: 'Demo Admin',
    accountId: 'a1',
    accountName: 'Demo Pharma Ltd',
    isPrimaryAdmin: true,
  },
  businessUnits: [ctg, dhaka],
  lastBusinessUnitId: 'dhk',
  theme: null,
  ...overrides,
});

const store = () => useAuthStore.getState();

describe('auth store', () => {
  beforeEach(() => useAuthStore.setState(signedOut));

  it('signs in without a business unit so the picker is shown', () => {
    store().signIn(login());

    expect(isSignedIn(store())).toBe(true);
    expect(store().currentBusinessUnitId).toBeNull();
    expect(store().lastBusinessUnitId).toBe('dhk');
  });

  it('treats an expired token as signed out', () => {
    store().signIn(login({ expiresAt: new Date(Date.now() - 1000).toISOString() }));

    expect(isSignedIn(store())).toBe(false);
  });

  it('selecting a unit stores it with its permissions', () => {
    store().signIn(login());
    store().selectUnit({ businessUnit: { ...ctg, roleName: 'Viewer' }, permissions: ['inventory.item.view'] });

    expect(selectCurrentUnit(store())?.roleName).toBe('Viewer');
    expect(store().permissions).toEqual(['inventory.item.view']);
    expect(store().lastBusinessUnitId).toBe('ctg');
  });

  it('refreshing the session drops a unit the user lost access to', () => {
    store().signIn(login());
    store().selectUnit({ businessUnit: ctg, permissions: ['inventory.item.view'] });

    store().refreshSession({ user: login().user, businessUnits: [dhaka], lastBusinessUnitId: 'dhk' });

    expect(store().currentBusinessUnitId).toBeNull();
    expect(store().permissions).toEqual([]);
  });

  it('refreshing the session keeps a unit that is still accessible', () => {
    store().signIn(login());
    store().selectUnit({ businessUnit: dhaka, permissions: [] });

    store().refreshSession({ user: login().user, businessUnits: [dhaka, ctg], lastBusinessUnitId: 'dhk' });

    expect(store().currentBusinessUnitId).toBe('dhk');
  });

  it('signing out clears everything', () => {
    store().signIn(login());
    store().signOut();

    expect(store()).toMatchObject(signedOut);
  });
});
