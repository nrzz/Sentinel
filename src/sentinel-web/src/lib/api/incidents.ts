import { api } from './client';

export type IncidentSeverity = 'low' | 'medium' | 'high' | 'critical';
export type IncidentStatus = 'open' | 'investigating' | 'mitigated' | 'resolved' | 'closed';

export interface Incident {
  id: string;
  title: string;
  description: string;
  severity: IncidentSeverity;
  status: IncidentStatus;
  assignedTo?: string;
  sourceAlertExecutionId?: string;
  createdBy?: string;
  resolvedAt?: string;
  createdAt: string;
  updatedAt: string;
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
