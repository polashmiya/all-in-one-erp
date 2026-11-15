import {
  isTokenActive,
  type BusinessUnitAccessDto,
  type ErpLoginResponse,
  type ErpSessionResponse,
  type ErpUserDto,
  type SelectUnitResponse,
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
  user: ErpUserDto | null;
  businessUnits: BusinessUnitAccessDto[];
  lastBusinessUnitId: string | null;
  currentBusinessUnitId: string | null;
  permissions: string[];
}

interface AuthState extends AuthData {
  signIn: (login: ErpLoginResponse) => void;
  setTokens: (tokens: SessionTokens) => void;
  refreshSession: (session: ErpSessionResponse) => void;
  selectUnit: (selection: SelectUnitResponse) => void;
  signOut: () => void;
}

export const signedOut: AuthData = {
  accessToken: null,
  expiresAt: null,
  refreshToken: null,
  refreshExpiresAt: null,
  mustChangePassword: false,
  user: null,
  businessUnits: [],
  lastBusinessUnitId: null,
  currentBusinessUnitId: null,
  permissions: [],
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
      signIn: (login) =>
        set({
          ...tokensOf(login),
          user: login.user,
          businessUnits: login.businessUnits,
          lastBusinessUnitId: login.lastBusinessUnitId,
          currentBusinessUnitId: null,
          permissions: [],
        }),
      setTokens: (tokens) => set(tokensOf(tokens)),
      refreshSession: (session) =>
        set((state) => {
          const stillAllowed = session.businessUnits.some(
            (u) => u.id === state.currentBusinessUnitId,
          );
          return {
            user: session.user,
            businessUnits: session.businessUnits,
            lastBusinessUnitId: session.lastBusinessUnitId,
            ...(stillAllowed ? {} : { currentBusinessUnitId: null, permissions: [] }),
          };
        }),
      selectUnit: (selection) =>
        set((state) => ({
          currentBusinessUnitId: selection.businessUnit.id,
          lastBusinessUnitId: selection.businessUnit.id,
          permissions: selection.permissions,
          businessUnits: state.businessUnits.map((u) =>
            u.id === selection.businessUnit.id ? selection.businessUnit : u,
          ),
        })),
      signOut: () => set(signedOut),
    }),
    { name: 'erp.auth' },
  ),
);

export const isSignedIn = (state: AuthData) =>
  isTokenActive(state.refreshToken, state.refreshExpiresAt);

export const selectCurrentUnit = (state: AuthData) =>
  state.businessUnits.find((u) => u.id === state.currentBusinessUnitId);
