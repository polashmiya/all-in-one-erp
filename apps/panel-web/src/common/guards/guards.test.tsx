import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router';
import { signedOut, useAuthStore } from '@/common/store/auth-store';
import { GuestOnly, RequireSignedIn } from './guards';

function renderAt(path: string, state?: unknown) {
  render(
    <MemoryRouter initialEntries={[{ pathname: path, state }]}>
      <Routes>
        <Route
          path="/login"
          element={
            <GuestOnly>
              <div>login</div>
            </GuestOnly>
          }
        />
        <Route path="/dashboard" element={<div>dashboard</div>} />
        <Route
          path="/tenants/*"
          element={
            <RequireSignedIn>
              <div>tenants</div>
            </RequireSignedIn>
          }
        />
      </Routes>
    </MemoryRouter>,
  );
}

const panelUser = {
  id: 'p1',
  email: 'superadmin@myerp.local',
  fullName: 'Super Admin',
  role: 'SuperAdmin',
};

describe('panel route guards', () => {
  beforeEach(() => useAuthStore.setState(signedOut));

  it('sends signed-out users to /login', () => {
    renderAt('/tenants/accounts');
    expect(screen.getByText('login')).toBeTruthy();
  });

  it('renders protected pages for a signed-in user', () => {
    useAuthStore.getState().signIn({
      accessToken: 't',
      expiresAt: new Date(Date.now() + 60_000).toISOString(),
      refreshToken: 'r',
      refreshExpiresAt: new Date(Date.now() + 60_000).toISOString(),
      mustChangePassword: false,
      user: panelUser,
    });
    renderAt('/tenants/accounts');
    expect(screen.getByText('tenants')).toBeTruthy();
  });

  it('moves a signed-in user off /login', () => {
    useAuthStore.getState().signIn({
      accessToken: 't',
      expiresAt: new Date(Date.now() + 60_000).toISOString(),
      refreshToken: 'r',
      refreshExpiresAt: new Date(Date.now() + 60_000).toISOString(),
      mustChangePassword: false,
      user: panelUser,
    });
    renderAt('/login', { from: '/dashboard' });
    expect(screen.getByText('dashboard')).toBeTruthy();
  });

  it('signing out ends the session', () => {
    useAuthStore.getState().signIn({
      accessToken: 't',
      expiresAt: new Date(Date.now() + 60_000).toISOString(),
      refreshToken: 'r',
      refreshExpiresAt: new Date(Date.now() + 60_000).toISOString(),
      mustChangePassword: false,
      user: panelUser,
    });
    useAuthStore.getState().signOut();
    renderAt('/tenants/accounts');
    expect(screen.getByText('login')).toBeTruthy();
  });
});
