import {
  isTokenActive,
  type PanelLoginResponse,
  type PanelUserDto,
  type SessionTokens,
} from '@erp/api-client';
import { create } from 'zustand';
import { persist } from 'zustand/middleware';

export interface AuthData {
  accessToken: string | null;
  expiresAt: string | null;
  refreshToken: string | null;
  refreshExpiresAt: string | null;
  mustChangePassword: boolean;
  user: PanelUserDto | null;
}

interface AuthState extends AuthData {
  signIn: (login: PanelLoginResponse) => void;
  setTokens: (tokens: SessionTokens) => void;
  setUser: (user: PanelUserDto) => void;
  signOut: () => void;
}

export const signedOut: AuthData = {
  accessToken: null,
  expiresAt: null,
  refreshToken: null,
  refreshExpiresAt: null,
  mustChangePassword: false,
  user: null,
};

const tokensOf = (tokens: SessionTokens) => ({
  accessToken: tokens.accessToken,
  expiresAt: tokens.expiresAt,
  refreshToken: tokens.refreshToken,
  refreshExpiresAt: tokens.refreshExpiresAt,
  mustChangePassword: tokens.mustChangePassword,
});

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      ...signedOut,
      signIn: (login) => set({ ...tokensOf(login), user: login.user }),
      setTokens: (tokens) => set(tokensOf(tokens)),
      setUser: (user) => set({ user }),
      signOut: () => set(signedOut),
    }),
    { name: 'panel.auth' },
  ),
);

export const isSignedIn = (state: AuthData) =>
  isTokenActive(state.refreshToken, state.refreshExpiresAt);
