import { useQuery } from '@tanstack/react-query';
import { listIncidents } from '@/lib/api/incidents';

const statusStyles: Record<string, string> = {
  open: 'bg-red-900/40 text-red-300',
  investigating: 'bg-yellow-900/40 text-yellow-300',
  mitigated: 'bg-blue-900/40 text-blue-300',
  resolved: 'bg-green-900/40 text-green-300',
};

export function IncidentsPage() {
  const { data: incidents = [], isLoading } = useQuery({
    queryKey: ['incidents'],
    queryFn: listIncidents,
  });

  return (
    <div className="space-y-4">
      <p className="text-sm text-zinc-400">Track and resolve production incidents.</p>

      <div className="overflow-hidden rounded-lg border border-zinc-800">
        <table className="w-full text-sm">
          <thead className="bg-zinc-900 text-left text-xs text-zinc-500">
            <tr>
              <th className="px-4 py-3 font-medium">Title</th>
              <th className="px-4 py-3 font-medium">Status</th>
              <th className="px-4 py-3 font-medium">Severity</th>
              <th className="px-4 py-3 font-medium">Assignee</th>
              <th className="px-4 py-3 font-medium">Created</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-zinc-800 bg-zinc-950">
            {isLoading ? (
              <tr>
                <td colSpan={5} className="px-4 py-8 text-center text-zinc-500">
                  Loading incidents...
                </td>
              </tr>
            ) : incidents.length === 0 ? (
              <tr>
                <td colSpan={5} className="px-4 py-8 text-center text-zinc-500">
                  No incidents recorded.
                </td>
              </tr>
            ) : (
              incidents.map((incident) => (
                <tr key={incident.id} className="hover:bg-zinc-900">
                  <td className="px-4 py-3 font-medium text-zinc-200">{incident.title}</td>
                  <td className="px-4 py-3">
                    <span
                      className={`rounded px-2 py-0.5 text-xs ${statusStyles[incident.status]}`}
                    >
                      {incident.status}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-zinc-400">{incident.severity}</td>
                  <td className="px-4 py-3 text-zinc-400">{incident.assignee ?? '—'}</td>
                  <td className="px-4 py-3 text-zinc-500">
                    {new Date(incident.createdAt).toLocaleString()}
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
