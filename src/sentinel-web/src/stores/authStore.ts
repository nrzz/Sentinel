import { create } from 'zustand';
import type { AuthTokens, LoginRequest, User } from '@/types/auth';
import * as authApi from '@/lib/api/auth';

interface AuthState {
  user: User | null;
  tokens: AuthTokens | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  error: string | null;
  login: (request: LoginRequest) => Promise<void>;
  logout: () => void;
  refresh: () => Promise<boolean>;
  getAccessToken: () => string | null;
}

function buildTokens(
  accessToken: string,
  refreshToken: string,
  expiresIn: number,
): AuthTokens {
  return {
    accessToken,
    refreshToken,
    expiresAt: Date.now() + expiresIn * 1000,
  };
}

export const useAuthStore = create<AuthState>((set, get) => ({
  user: null,
  tokens: null,
  isAuthenticated: false,
  isLoading: false,
  error: null,

  login: async (request) => {
    set({ isLoading: true, error: null });
    try {
      const response = await authApi.login(request);
      set({
        user: response.user,
        tokens: buildTokens(
          response.accessToken,
          response.refreshToken,
          response.expiresIn,
        ),
        isAuthenticated: true,
        isLoading: false,
      });
    } catch (err) {
      set({
        error: err instanceof Error ? err.message : 'Login failed',
        isLoading: false,
      });
      throw err;
    }
  },

  logout: () => {
    set({
      user: null,
      tokens: null,
      isAuthenticated: false,
      error: null,
    });
  },

  refresh: async () => {
    const { tokens } = get();
    if (!tokens?.refreshToken) return false;

    try {
      const response = await authApi.refresh(tokens.refreshToken);
      set({
        tokens: buildTokens(
          response.accessToken,
          response.refreshToken,
          response.expiresIn,
        ),
        isAuthenticated: true,
      });
      return true;
    } catch {
      get().logout();
      return false;
    }
  },

  getAccessToken: () => {
    const { tokens } = get();
    if (!tokens) return null;

    if (tokens.expiresAt - Date.now() < 60_000) {
      void get().refresh();
    }

    return tokens.accessToken;
  },
}));
