export interface SentinelClientOptions {
  baseUrl?: string;
  accessToken?: string;
  refreshToken?: string;
  tenantId?: string;
  tenantHeaderName?: string;
  timeoutMs?: number;
  maxRetryAttempts?: number;
}

export interface LoginRequest {
  email: string;
  password: string;
  tenantId?: string;
}

export interface AuthUser {
  id: string;
  email: string;
  displayName: string;
  tenantId: string;
  roles: string[];
  permissions: string[];
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAt: string;
  user: AuthUser;
}

export interface LogEntryInput {
  service: string;
  environment: string;
  level: string;
  message: string;
  attributes?: Record<string, string>;
  traceId?: string;
  spanId?: string;
  correlationId?: string;
  timestamp?: string;
}

export interface IngestLogsResponse {
  acceptedCount: number;
  status: string;
}

export interface LogSearchQuery {
  from?: string;
  to?: string;
  level?: string;
  service?: string;
  environment?: string;
  query?: string;
  traceId?: string;
  limit?: number;
  offset?: number;
}

export interface LogSearchItem {
  id: string;
  timestamp: string;
  service: string;
  environment: string;
  level: string;
  normalizedLevel: string;
  message: string;
  traceId?: string;
  spanId?: string;
  correlationId?: string;
  parsedException?: string;
  sourceHost?: string;
}

export interface SearchLogsResponse {
  items: LogSearchItem[];
  totalCount: number;
  limit: number;
  offset: number;
}

export interface MetricEntryInput {
  name: string;
  value: number;
  unit: string;
  tags?: Record<string, string>;
  service: string;
  environment: string;
  timestamp?: string;
}

export interface IngestMetricsResponse {
  acceptedCount: number;
  status: string;
}

export interface MetricQuery {
  from?: string;
  to?: string;
  name?: string;
  service?: string;
  environment?: string;
  limit?: number;
}

export interface MetricItem {
  id: string;
  timestamp: string;
  name: string;
  value: number;
  unit: string;
  service: string;
  environment: string;
  tags: Record<string, string>;
}

export interface QueryMetricsResponse {
  items: MetricItem[];
}

export type AlertSeverity = 'info' | 'warning' | 'critical';
export type AlertRuleStatus = 'active' | 'paused' | 'disabled';

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

export interface Tenant {
  id: string;
  name: string;
  slug: string;
  isActive: boolean;
  environment: string;
  settingsJson: string;
  createdAt: string;
  updatedAt: string;
}

export class SentinelApiError extends Error {
  constructor(
    public readonly statusCode: number,
    message: string,
    public readonly responseBody?: string
  ) {
    super(`Sentinel API error ${statusCode}: ${message}`);
    this.name = 'SentinelApiError';
  }
}
