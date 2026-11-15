import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes, useLocation } from 'react-router';
import { signedOut, useAuthStore } from '@/common/store/auth-store';
import { GuestOnly, RequireBusinessUnit, RequireSignedIn } from './guards';
import { redirectTarget } from './redirect';

function Where({ label }: { label: string }) {
  const location = useLocation();
  return (
    <div>
      {label} from={String((location.state as { from?: string } | null)?.from ?? '')}
    </div>
  );
}

function renderAt(path: string, state?: unknown) {
  render(
    <MemoryRouter initialEntries={[{ pathname: path, state }]}>
      <Routes>
        <Route
          path="/login"
          element={
            <GuestOnly>
              <Where label="login" />
            </GuestOnly>
          }
        />
        <Route path="/select-unit" element={<Where label="picker" />} />
        <Route path="/change-password" element={<Where label="change-password" />} />
        <Route path="/dashboard" element={<Where label="dashboard" />} />
        <Route
          path="/app/*"
          element={
            <RequireSignedIn>
              <RequireBusinessUnit>
                <Where label="feature" />
              </RequireBusinessUnit>
            </RequireSignedIn>
          }
        />
      </Routes>
    </MemoryRouter>,
  );
}

const future = () => new Date(Date.now() + 3_600_000).toISOString();

describe('route guards', () => {
  beforeEach(() => useAuthStore.setState(signedOut));

  it('sends signed-out users to /login and remembers the target', () => {
    renderAt('/app/inventory/item');
    expect(screen.getByText('login from=/app/inventory/item')).toBeTruthy();
  });

  it('sends users with an expired session to /login', () => {
    useAuthStore.setState({
      refreshToken: 't',
      refreshExpiresAt: new Date(Date.now() - 1).toISOString(),
      currentBusinessUnitId: 'dhk',
    });
    renderAt('/app/inventory/item');
    expect(screen.getByText(/^login/)).toBeTruthy();
  });

  it('sends signed-in users without a unit to the picker', () => {
    useAuthStore.setState({ refreshToken: 't', refreshExpiresAt: future() });
    renderAt('/app/inventory/item');
    expect(screen.getByText('picker from=/app/inventory/item')).toBeTruthy();
  });

  it('renders the page once signed in with a unit', () => {
    useAuthStore.setState({
      refreshToken: 't',
      refreshExpiresAt: future(),
      currentBusinessUnitId: 'dhk',
    });
    renderAt('/app/inventory/item');
    expect(screen.getByText(/^feature/)).toBeTruthy();
  });

  it('sends users who must change their password to the change-password page', () => {
    useAuthStore.setState({
      refreshToken: 't',
      refreshExpiresAt: future(),
      mustChangePassword: true,
      currentBusinessUnitId: 'dhk',
    });
    renderAt('/app/inventory/item');
    expect(screen.getByText('change-password from=/app/inventory/item')).toBeTruthy();
  });

  it('moves signed-in users off /login to where they were going', () => {
    useAuthStore.setState({
      refreshToken: 't',
      refreshExpiresAt: future(),
      currentBusinessUnitId: 'dhk',
    });
    renderAt('/login', { from: '/dashboard' });
    expect(screen.getByText(/^dashboard/)).toBeTruthy();
  });
});

describe('redirectTarget', () => {
  it('accepts in-app paths only', () => {
    expect(redirectTarget({ from: '/app/inventory/item?x=1' })).toBe('/app/inventory/item?x=1');
    expect(redirectTarget({ from: '//evil.example' })).toBe('/dashboard');
    expect(redirectTarget({ from: 'https://evil.example' })).toBe('/dashboard');
    expect(redirectTarget({ from: '/login' })).toBe('/dashboard');
    expect(redirectTarget(null)).toBe('/dashboard');
  });
});
