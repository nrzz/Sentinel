import { api } from './client';

export interface TraceSummary {
  traceId: string;
  service: string;
  operation: string;
  durationMs: number;
  startTime: string;
  status: string;
  spanCount: number;
}

export interface Span {
  spanId: string;
  parentSpanId?: string;
  operation: string;
  service: string;
  startTime: string;
  durationMs: number;
  status: string;
  attributes?: Record<string, string>;
}

export interface TraceDetail {
  traceId: string;
  spans: Span[];
}

const BASE = '/api/v1/traces';

export function queryTraces(params: Record<string, string> = {}): Promise<TraceSummary[]> {
  const search = new URLSearchParams(params);
  const qs = search.toString();
  return api.get<TraceSummary[]>(`${BASE}${qs ? `?${qs}` : ''}`);
}

export function getTrace(traceId: string): Promise<TraceDetail> {
  return api.get<TraceDetail>(`${BASE}/${traceId}`);
}
