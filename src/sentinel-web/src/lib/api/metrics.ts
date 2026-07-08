import { api } from './client';

export interface MetricSeries {
  name: string;
  labels: Record<string, string>;
  dataPoints: { timestamp: string; value: number }[];
}

export interface MetricQueryParams {
  query?: string;
  from?: string;
  to?: string;
  step?: string;
}

const BASE = '/api/v1/metrics';

export function queryMetrics(params: MetricQueryParams = {}): Promise<MetricSeries[]> {
  const search = new URLSearchParams();
  Object.entries(params).forEach(([key, value]) => {
    if (value !== undefined && value !== '') {
      search.set(key, String(value));
    }
  });
  const qs = search.toString();
  return api.get<MetricSeries[]>(`${BASE}/query${qs ? `?${qs}` : ''}`);
}

export function listMetricNames(): Promise<string[]> {
  return api.get<string[]>(`${BASE}/names`);
}
