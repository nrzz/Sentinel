import { useQuery } from '@tanstack/react-query';
import { listDashboards } from '@/lib/api/dashboards';

export function DashboardsPage() {
  const { data: dashboards = [], isLoading } = useQuery({
    queryKey: ['dashboards'],
    queryFn: listDashboards,
  });

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <p className="text-sm text-zinc-400">Custom dashboards for your observability data.</p>
        <button
          type="button"
          className="rounded-md bg-sentinel-600 px-4 py-2 text-sm text-white hover:bg-sentinel-500"
        >
          New Dashboard
        </button>
      </div>

      {isLoading ? (
        <p className="text-sm text-zinc-500">Loading dashboards...</p>
      ) : dashboards.length === 0 ? (
        <div className="rounded-lg border border-zinc-800 bg-zinc-900 p-8 text-center">
          <p className="text-sm text-zinc-500">No dashboards yet.</p>
          <p className="mt-1 text-xs text-zinc-600">Create a dashboard to visualize your data.</p>
        </div>
      ) : (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {dashboards.map((dashboard) => (
            <div
              key={dashboard.id}
              className="cursor-pointer rounded-lg border border-zinc-800 bg-zinc-900 p-4 hover:border-sentinel-700"
            >
              <h3 className="font-medium text-zinc-200">{dashboard.name}</h3>
              {dashboard.description && (
                <p className="mt-1 text-sm text-zinc-500">{dashboard.description}</p>
              )}
              <p className="mt-3 text-xs text-zinc-600">
                {dashboard.panels.length} panel{dashboard.panels.length !== 1 ? 's' : ''}
              </p>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
