import { useQuery } from '@tanstack/react-query';
import { queryTraces } from '@/lib/api/traces';

const statusColors: Record<string, string> = {
  ok: 'text-green-400',
  error: 'text-red-400',
  unset: 'text-zinc-400',
};

export function TracesPage() {
  const { data: traces = [], isLoading } = useQuery({
    queryKey: ['traces'],
    queryFn: () => queryTraces(),
  });

  return (
    <div className="space-y-4">
      <p className="text-sm text-zinc-400">Distributed traces across your services.</p>

      <div className="overflow-hidden rounded-lg border border-zinc-800">
        <table className="w-full text-sm">
          <thead className="bg-zinc-900 text-left text-xs text-zinc-500">
            <tr>
              <th className="px-4 py-3 font-medium">Trace ID</th>
              <th className="px-4 py-3 font-medium">Service</th>
              <th className="px-4 py-3 font-medium">Operation</th>
              <th className="px-4 py-3 font-medium">Duration</th>
              <th className="px-4 py-3 font-medium">Spans</th>
              <th className="px-4 py-3 font-medium">Status</th>
              <th className="px-4 py-3 font-medium">Time</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-zinc-800 bg-zinc-950">
            {isLoading ? (
              <tr>
                <td colSpan={7} className="px-4 py-8 text-center text-zinc-500">
                  Loading traces...
                </td>
              </tr>
            ) : traces.length === 0 ? (
              <tr>
                <td colSpan={7} className="px-4 py-8 text-center text-zinc-500">
                  No traces found.
                </td>
              </tr>
            ) : (
              traces.map((trace) => (
                <tr key={trace.traceId} className="hover:bg-zinc-900">
                  <td className="px-4 py-3 font-mono text-xs text-sentinel-400">
                    {trace.traceId.slice(0, 12)}…
                  </td>
                  <td className="px-4 py-3 text-zinc-300">{trace.service}</td>
                  <td className="px-4 py-3 text-zinc-300">{trace.operation}</td>
                  <td className="px-4 py-3 text-zinc-300">{trace.durationMs}ms</td>
                  <td className="px-4 py-3 text-zinc-400">{trace.spanCount}</td>
                  <td className={`px-4 py-3 ${statusColors[trace.status] ?? 'text-zinc-400'}`}>
                    {trace.status}
                  </td>
                  <td className="px-4 py-3 text-zinc-500">
                    {new Date(trace.startTime).toLocaleString()}
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
