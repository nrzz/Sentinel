import { useQuery } from '@tanstack/react-query';
import { listAlerts } from '@/lib/api/alerts';
import { listIncidents } from '@/lib/api/incidents';

function StatCard({ label, value, subtext }: { label: string; value: string | number; subtext?: string }) {
  return (
    <div className="rounded-lg border border-zinc-800 bg-zinc-900 p-4">
      <p className="text-xs text-zinc-500">{label}</p>
      <p className="mt-1 text-2xl font-semibold text-zinc-100">{value}</p>
      {subtext && <p className="mt-1 text-xs text-zinc-500">{subtext}</p>}
    </div>
  );
}

export function OverviewPage() {
  const { data: alerts = [] } = useQuery({ queryKey: ['alerts'], queryFn: listAlerts });
  const { data: incidents = [] } = useQuery({ queryKey: ['incidents'], queryFn: listIncidents });

  const firingAlerts = alerts.filter((a) => a.status === 'firing').length;
  const openIncidents = incidents.filter((i) => i.status !== 'resolved').length;

  return (
    <div className="space-y-6">
      <p className="text-sm text-zinc-400">
        Real-time observability across logs, metrics, traces, and alerts.
      </p>
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <StatCard label="Firing Alerts" value={firingAlerts} />
        <StatCard label="Open Incidents" value={openIncidents} />
        <StatCard label="Services" value="—" subtext="Connect agents to populate" />
        <StatCard label="Ingestion Rate" value="—" subtext="Logs / sec" />
      </div>
      <div className="rounded-lg border border-zinc-800 bg-zinc-900 p-6">
        <h2 className="mb-4 text-sm font-medium text-zinc-300">Recent Alerts</h2>
        {alerts.length === 0 ? (
          <p className="text-sm text-zinc-500">No alerts yet. Configure alert rules to get started.</p>
        ) : (
          <ul className="space-y-2">
            {alerts.slice(0, 5).map((alert) => (
              <li
                key={alert.id}
                className="flex items-center justify-between rounded-md bg-zinc-950 px-3 py-2 text-sm"
              >
                <span className="text-zinc-300">{alert.name}</span>
                <span
                  className={`rounded px-2 py-0.5 text-xs ${
                    alert.severity === 'critical'
                      ? 'bg-red-900/50 text-red-300'
                      : alert.severity === 'warning'
                        ? 'bg-yellow-900/50 text-yellow-300'
                        : 'bg-blue-900/50 text-blue-300'
                  }`}
                >
                  {alert.status}
                </span>
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  );
}
