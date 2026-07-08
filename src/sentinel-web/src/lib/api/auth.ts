import { api } from './client';
import type { LoginRequest, LoginResponse, RefreshResponse } from '@/types/auth';

const BASE = '/api/v1/auth';

export function login(request: LoginRequest): Promise<LoginResponse> {
  return api.post<LoginResponse>(`${BASE}/login`, request, { skipAuth: true });
}

export function refresh(refreshToken: string): Promise<RefreshResponse> {
  return api.post<RefreshResponse>(
    `${BASE}/refresh`,
    { refreshToken },
    { skipAuth: true },
  );
}

export function logout(): Promise<void> {
  return api.post<void>(`${BASE}/logout`);
}
