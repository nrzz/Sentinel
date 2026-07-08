import { api } from './client';

export interface Plugin {
  id: string;
  name: string;
  version: string;
  description: string;
  enabled: boolean;
  author: string;
}

const BASE = '/api/v1/plugins';

export function listPlugins(): Promise<Plugin[]> {
  return api.get<Plugin[]>(BASE);
}

export function togglePlugin(id: string, enabled: boolean): Promise<Plugin> {
  return api.patch<Plugin>(`${BASE}/${id}`, { enabled });
}
