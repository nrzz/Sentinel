import { api } from './client';
import type { LogEntry, LogQueryParams, PagedResponse } from '@/types/api';

const BASE = '/api/v1/search/logs';

export function queryLogs(params: LogQueryParams = {}): Promise<PagedResponse<LogEntry>> {
  const search = new URLSearchParams();
  if (params.query) search.set('query', params.query);
  if (params.level) search.set('level', params.level);
  if (params.service) search.set('service', params.service);
  if (params.from) search.set('from', params.from);
  if (params.to) search.set('to', params.to);
  if (params.pageSize) search.set('limit', String(params.pageSize));
  if (params.page && params.pageSize) {
    search.set('offset', String((params.page - 1) * params.pageSize));
  }

  const qs = search.toString();
  return api.get<SearchLogsApiResponse>(`${BASE}${qs ? `?${qs}` : ''}`).then((response) => ({
    items: response.items.map(mapLogItem),
    totalCount: response.totalCount,
    page: params.page ?? 1,
    pageSize: response.limit,
  }));
}

interface SearchLogsApiResponse {
  items: Array<{
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
  }>;
  totalCount: number;
  limit: number;
  offset: number;
}

function mapLogItem(item: SearchLogsApiResponse['items'][number]): LogEntry {
  return {
    id: item.id,
    timestamp: item.timestamp,
    level: item.level,
    message: item.message,
    service: item.service,
    traceId: item.traceId,
  };
}

export function getLog(id: string): Promise<LogEntry> {
  return api.get<LogEntry>(`/api/v1/logs/${id}`);
}
