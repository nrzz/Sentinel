import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { isPluginEnabled, listPlugins, togglePlugin } from '@/lib/api/plugins';

export function PluginsPage() {
  const queryClient = useQueryClient();
  const { data: plugins = [], isLoading } = useQuery({
    queryKey: ['plugins'],
    queryFn: listPlugins,
  });

  const toggle = useMutation({
    mutationFn: ({ id, enabled }: { id: string; enabled: boolean }) =>
      togglePlugin(id, enabled),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['plugins'] }),
  });

  return (
    <div className="space-y-4">
      <p className="text-sm text-zinc-400">Extend Sentinel with plugins and integrations.</p>

      {isLoading ? (
        <p className="text-sm text-zinc-500">Loading plugins...</p>
      ) : plugins.length === 0 ? (
        <div className="rounded-lg border border-zinc-800 bg-zinc-900 p-8 text-center">
          <p className="text-sm text-zinc-500">No plugins installed.</p>
        </div>
      ) : (
        <div className="space-y-3">
          {plugins.map((plugin) => {
            const enabled = isPluginEnabled(plugin);
            return (
              <div
                key={plugin.id}
                className="flex items-center justify-between rounded-lg border border-zinc-800 bg-zinc-900 p-4"
              >
                <div>
                  <div className="flex items-center gap-2">
                    <span className="font-medium text-zinc-200">{plugin.name}</span>
                    <span className="text-xs text-zinc-600">v{plugin.version}</span>
                    <span className="text-xs text-zinc-600">{plugin.status}</span>
                  </div>
                  <p className="mt-1 text-sm text-zinc-400">{plugin.description}</p>
                  {plugin.installedBy && (
                    <p className="mt-1 text-xs text-zinc-600">installed by {plugin.installedBy}</p>
                  )}
                </div>
                <button
                  type="button"
                  onClick={() => toggle.mutate({ id: plugin.id, enabled: !enabled })}
                  disabled={toggle.isPending}
                  className={`rounded-md px-4 py-1.5 text-sm ${
                    enabled
                      ? 'bg-sentinel-900 text-sentinel-300 border border-sentinel-700'
                      : 'border border-zinc-700 text-zinc-400 hover:bg-zinc-800'
                  }`}
                >
                  {enabled ? 'Enabled' : 'Disabled'}
                </button>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}
