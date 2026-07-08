export interface LogEntry {
  id: string;
  timestamp: string;
  level: string;
  message: string;
  service: string;
  traceId?: string;
  attributes?: Record<string, string>;
}

export interface LogQueryParams {
  query?: string;
  level?: string;
  service?: string;
  from?: string;
  to?: string;
  page?: number;
  pageSize?: number;
}

export interface PagedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}
