import { api } from './client';

export interface Alert {
  id: string;
  name: string;
  severity: 'critical' | 'warning' | 'info';
  status: 'firing' | 'resolved' | 'silenced';
  message: string;
  firedAt: string;
  resolvedAt?: string;
  labels?: Record<string, string>;
}

const BASE = '/api/v1/alerts';

export function listAlerts(): Promise<Alert[]> {
  return api.get<Alert[]>(BASE);
}

export function silenceAlert(id: string, durationMinutes: number): Promise<void> {
  return api.post<void>(`${BASE}/${id}/silence`, { durationMinutes });
}
