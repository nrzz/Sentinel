import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { listAlerts, silenceAlert } from '@/lib/api/alerts';

const severityStyles: Record<string, string> = {
  critical: 'bg-red-900/40 text-red-300 border-red-800',
  warning: 'bg-yellow-900/40 text-yellow-300 border-yellow-800',
  info: 'bg-blue-900/40 text-blue-300 border-blue-800',
};

export function AlertsPage() {
  const queryClient = useQueryClient();
  const { data: alerts = [], isLoading } = useQuery({
    queryKey: ['alerts'],
    queryFn: listAlerts,
  });

  const silence = useMutation({
    mutationFn: (id: string) => silenceAlert(id, 60),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['alerts'] }),
  });

  return (
    <div className="space-y-4">
      <p className="text-sm text-zinc-400">Active and resolved alert rules.</p>

      {isLoading ? (
        <p className="text-sm text-zinc-500">Loading alerts...</p>
      ) : alerts.length === 0 ? (
        <div className="rounded-lg border border-zinc-800 bg-zinc-900 p-8 text-center">
          <p className="text-sm text-zinc-500">No alerts configured.</p>
        </div>
      ) : (
        <div className="space-y-3">
          {alerts.map((alert) => (
            <div
              key={alert.id}
              className="flex items-center justify-between rounded-lg border border-zinc-800 bg-zinc-900 p-4"
            >
              <div className="space-y-1">
                <div className="flex items-center gap-2">
                  <span className="font-medium text-zinc-200">{alert.name}</span>
                  <span
                    className={`rounded border px-2 py-0.5 text-xs ${severityStyles[alert.severity]}`}
                  >
                    {alert.severity}
                  </span>
                  <span className="text-xs text-zinc-500">{alert.status}</span>
                </div>
                <p className="text-sm text-zinc-400">{alert.message}</p>
                <p className="text-xs text-zinc-600">
                  Fired {new Date(alert.firedAt).toLocaleString()}
                </p>
              </div>
              {alert.status === 'firing' && (
                <button
                  type="button"
                  onClick={() => silence.mutate(alert.id)}
                  disabled={silence.isPending}
                  className="rounded-md border border-zinc-700 px-3 py-1.5 text-xs text-zinc-400 hover:bg-zinc-800"
                >
                  Silence 1h
                </button>
              )}
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
