import { api } from './client';

export interface Incident {
  id: string;
  title: string;
  status: 'open' | 'investigating' | 'mitigated' | 'resolved';
  severity: 'critical' | 'high' | 'medium' | 'low';
  createdAt: string;
  updatedAt: string;
  assignee?: string;
  alertIds: string[];
}

const BASE = '/api/v1/incidents';

export function listIncidents(): Promise<Incident[]> {
  return api.get<Incident[]>(BASE);
}

export function getIncident(id: string): Promise<Incident> {
  return api.get<Incident>(`${BASE}/${id}`);
}

export function updateIncidentStatus(
  id: string,
  status: Incident['status'],
): Promise<Incident> {
  return api.patch<Incident>(`${BASE}/${id}`, { status });
}
