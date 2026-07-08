import {
  AlertRule,
  AuthResponse,
  Incident,
  IngestLogsResponse,
  IngestMetricsResponse,
  LogEntryInput,
  LoginRequest,
  MetricEntryInput,
  MetricQuery,
  QueryMetricsResponse,
  SearchLogsResponse,
  SentinelApiError,
  SentinelClientOptions,
  Tenant,
  LogSearchQuery,
} from './types.js';

const DEFAULT_BASE_URL = 'http://localhost:5000';
const DEFAULT_TIMEOUT_MS = 30_000;
const DEFAULT_MAX_RETRIES = 3;

function sleep(ms: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

function buildQuery(params: Record<string, string | number | undefined>): string {
  const search = new URLSearchParams();
  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== null && `${value}`.length > 0) {
      search.set(key, String(value));
    }
  }
  const query = search.toString();
  return query ? `?${query}` : '';
}

function parseErrorMessage(body: string): string | undefined {
  try {
    const parsed = JSON.parse(body) as { error?: string; title?: string };
    return parsed.error ?? parsed.title;
  } catch {
    return undefined;
  }
}

export class SentinelClient {
  private readonly options: Required<Pick<SentinelClientOptions, 'baseUrl' | 'tenantHeaderName' | 'timeoutMs' | 'maxRetryAttempts'>> &
    SentinelClientOptions;

  constructor(options: SentinelClientOptions = {}) {
    this.options = {
      baseUrl: options.baseUrl ?? DEFAULT_BASE_URL,
      tenantHeaderName: options.tenantHeaderName ?? 'X-Tenant-ID',
      timeoutMs: options.timeoutMs ?? DEFAULT_TIMEOUT_MS,
      maxRetryAttempts: options.maxRetryAttempts ?? DEFAULT_MAX_RETRIES,
      ...options,
    };
  }

  setAccessToken(accessToken: string, tenantId?: string): void {
    this.options.accessToken = accessToken;
    if (tenantId) {
      this.options.tenantId = tenantId;
    }
  }

  async login(request: LoginRequest): Promise<AuthResponse> {
    const response = await this.post<AuthResponse>('/api/v1/auth/login', request, false);
    this.options.accessToken = response.accessToken;
    this.options.refreshToken = response.refreshToken;
    this.options.tenantId = response.user.tenantId;
    return response;
  }

  async refreshToken(): Promise<AuthResponse> {
    if (!this.options.refreshToken) {
      throw new Error('Refresh token is not configured.');
    }

    const response = await this.post<AuthResponse>(
      '/api/v1/auth/refresh',
      { refreshToken: this.options.refreshToken },
      false
    );
    this.options.accessToken = response.accessToken;
    this.options.refreshToken = response.refreshToken;
    this.options.tenantId = response.user.tenantId;
    return response;
  }

  async ingestLogs(logs: LogEntryInput[]): Promise<IngestLogsResponse> {
    return this.post<IngestLogsResponse>('/api/v1/logs', { logs }, true);
  }

  async searchLogs(query: LogSearchQuery = {}): Promise<SearchLogsResponse> {
    const path = `/api/v1/search/logs${buildQuery({
      from: query.from,
      to: query.to,
      level: query.level,
      service: query.service,
      environment: query.environment,
      query: query.query,
      traceId: query.traceId,
      limit: query.limit,
      offset: query.offset,
    })}`;
    return this.get<SearchLogsResponse>(path);
  }

  async ingestMetrics(metrics: MetricEntryInput[]): Promise<IngestMetricsResponse> {
    return this.post<IngestMetricsResponse>('/api/v1/metrics', { metrics }, true);
  }

  async queryMetrics(query: MetricQuery = {}): Promise<QueryMetricsResponse> {
    const path = `/api/v1/metrics${buildQuery({
      from: query.from,
      to: query.to,
      name: query.name,
      service: query.service,
      environment: query.environment,
      limit: query.limit,
    })}`;
    return this.get<QueryMetricsResponse>(path);
  }

  async listAlerts(): Promise<AlertRule[]> {
    return this.get<AlertRule[]>('/api/v1/alerts');
  }

  async getAlert(id: string): Promise<AlertRule> {
    return this.get<AlertRule>(`/api/v1/alerts/${id}`);
  }

  async listIncidents(): Promise<Incident[]> {
    return this.get<Incident[]>('/api/v1/incidents');
  }

  async getIncident(id: string): Promise<Incident> {
    return this.get<Incident>(`/api/v1/incidents/${id}`);
  }

  async listTenants(): Promise<Tenant[]> {
    return this.get<Tenant[]>('/api/v1/tenants');
  }

  async getTenant(id: string): Promise<Tenant> {
    return this.get<Tenant>(`/api/v1/tenants/${id}`);
  }

  private async get<T>(path: string): Promise<T> {
    return this.request<T>('GET', path, undefined, true);
  }

  private async post<T>(path: string, body: unknown, authenticated: boolean): Promise<T> {
    return this.request<T>('POST', path, body, authenticated);
  }

  private async request<T>(
    method: string,
    path: string,
    body: unknown | undefined,
    authenticated: boolean
  ): Promise<T> {
    const url = `${this.options.baseUrl.replace(/\/$/, '')}${path}`;
    let lastError: Error | undefined;

    for (let attempt = 0; attempt <= this.options.maxRetryAttempts; attempt++) {
      const controller = new AbortController();
      const timeout = setTimeout(() => controller.abort(), this.options.timeoutMs);

      try {
        const headers: Record<string, string> = {
          Accept: 'application/json',
        };

        if (body !== undefined) {
          headers['Content-Type'] = 'application/json';
        }

        if (authenticated && this.options.accessToken) {
          headers.Authorization = `Bearer ${this.options.accessToken}`;
        }

        if (authenticated && this.options.tenantId) {
          headers[this.options.tenantHeaderName] = this.options.tenantId;
        }

        const response = await fetch(url, {
          method,
          headers,
          body: body === undefined ? undefined : JSON.stringify(body),
          signal: controller.signal,
        });

        const responseBody = await response.text();
        if (response.ok) {
          return JSON.parse(responseBody) as T;
        }

        if (response.status === 429 || response.status >= 500) {
          lastError = new SentinelApiError(
            response.status,
            parseErrorMessage(responseBody) ?? response.statusText,
            responseBody
          );
          await sleep(Math.pow(2, attempt + 1) * 100);
          continue;
        }

        throw new SentinelApiError(
          response.status,
          parseErrorMessage(responseBody) ?? response.statusText,
          responseBody
        );
      } catch (error) {
        if (error instanceof SentinelApiError) {
          throw error;
        }

        lastError = error instanceof Error ? error : new Error(String(error));
        if (attempt < this.options.maxRetryAttempts) {
          await sleep(Math.pow(2, attempt + 1) * 100);
          continue;
        }
      } finally {
        clearTimeout(timeout);
      }
    }

    throw lastError ?? new Error('Request failed.');
  }
}
