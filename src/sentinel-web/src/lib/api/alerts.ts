import { api } from './client';

export type AlertSeverity = 'info' | 'warning' | 'critical';
export type AlertRuleStatus = 'active' | 'paused' | 'disabled';
export type AlertExecutionStatus = 'triggered' | 'resolved' | 'suppressed' | 'failed';

export interface AlertRule {
  id: string;
  tenantId: string;
  name: string;
  description: string;
  query: string;
  condition: string;
  severity: AlertSeverity;
  status: AlertRuleStatus;
  evaluationIntervalSeconds: number;
  notificationChannels: string[];
  createdBy?: string;
  createdAt: string;
  updatedAt: string;
}

export interface AlertExecution {
  id: string;
  alertRuleId: string;
  status: AlertExecutionStatus;
  severity: AlertSeverity;
  message: string;
  matchedValue?: string;
  triggeredAt: string;
  resolvedAt?: string;
  correlationId?: string;
  createdAt: string;
}

const BASE = '/api/v1/alerts';

export function listAlertRules(): Promise<AlertRule[]> {
  return api.get<AlertRule[]>(BASE);
}

export function listAlertExecutions(limit = 50): Promise<AlertExecution[]> {
  return api.get<AlertExecution[]>(`${BASE}/executions?limit=${limit}`);
}

export function silenceAlertExecution(id: string, durationMinutes: number): Promise<AlertExecution> {
  return api.post<AlertExecution>(`${BASE}/executions/${id}/silence`, { durationMinutes });
}
