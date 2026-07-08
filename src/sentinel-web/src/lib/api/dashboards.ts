import { api } from './client';

export interface Dashboard {
  id: string;
  name: string;
  description?: string;
  panels: DashboardPanel[];
  createdAt: string;
  updatedAt: string;
}

export interface DashboardPanel {
  id: string;
  title: string;
  type: 'metric' | 'log' | 'trace' | 'alert';
  query: string;
}

const BASE = '/api/v1/dashboards';

export function listDashboards(): Promise<Dashboard[]> {
  return api.get<Dashboard[]>(BASE);
}

export function getDashboard(id: string): Promise<Dashboard> {
  return api.get<Dashboard>(`${BASE}/${id}`);
}
