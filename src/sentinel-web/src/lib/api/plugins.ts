import { api } from './client';

export type PluginStatus = 'installed' | 'enabled' | 'disabled' | 'failed';

export interface Plugin {
  id: string;
  name: string;
  version: string;
  description: string;
  assemblyName: string;
  configurationJson: string;
  status: PluginStatus;
  installedBy?: string;
  installedAt: string;
  createdAt: string;
}

const BASE = '/api/v1/plugins';

export function listPlugins(): Promise<Plugin[]> {
  return api.get<Plugin[]>(BASE);
}

export function togglePlugin(id: string, enabled: boolean): Promise<Plugin> {
  return api.patch<Plugin>(`${BASE}/${id}`, { enabled });
}

export function isPluginEnabled(plugin: Plugin): boolean {
  return plugin.status === 'enabled';
}
