import { useCallback, useEffect, useRef, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useVirtualizer } from '@tanstack/react-virtual';
import { queryLogs } from '@/lib/api/logs';
import { connectLogsHub, disconnectLogsHub } from '@/lib/signalr/logsHub';
import { useAuthStore } from '@/stores/authStore';
import type { LogEntry } from '@/types/api';

const levelColors: Record<string, string> = {
  error: 'text-red-400',
  warn: 'text-yellow-400',
  warning: 'text-yellow-400',
  info: 'text-blue-400',
  debug: 'text-zinc-500',
};

export function LogsPage() {
  const getAccessToken = useAuthStore((s) => s.getAccessToken);
  const tenantId = useAuthStore((s) => s.user?.tenantId ?? null);
  const [liveLogs, setLiveLogs] = useState<LogEntry[]>([]);
  const [query, setQuery] = useState('');
  const [level, setLevel] = useState('');
  const parentRef = useRef<HTMLDivElement>(null);

  const { data, isLoading, refetch } = useQuery({
    queryKey: ['logs', query, level],
    queryFn: () => queryLogs({ query, level, pageSize: 500 }),
  });

  const logs = [...liveLogs, ...(data?.items ?? [])];

  const rowVirtualizer = useVirtualizer({
    count: logs.length,
    getScrollElement: () => parentRef.current,
    estimateSize: () => 36,
    overscan: 20,
  });

  const handleLiveLog = useCallback((log: LogEntry) => {
    setLiveLogs((prev) => [log, ...prev].slice(0, 200));
  }, []);

  useEffect(() => {
    void connectLogsHub(getAccessToken, tenantId, handleLiveLog);
    return () => {
      void disconnectLogsHub();
    };
  }, [getAccessToken, tenantId, handleLiveLog]);

  return (
    <div className="flex h-[calc(100vh-8rem)] flex-col gap-4">
      <div className="flex gap-3">
        <input
          type="text"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          placeholder="Search logs..."
          className="flex-1 rounded-md border border-zinc-700 bg-zinc-900 px-3 py-2 text-sm text-zinc-100 outline-none focus:border-sentinel-600"
        />
        <select
          value={level}
          onChange={(e) => setLevel(e.target.value)}
          className="rounded-md border border-zinc-700 bg-zinc-900 px-3 py-2 text-sm text-zinc-100"
        >
          <option value="">All levels</option>
          <option value="error">Error</option>
          <option value="warn">Warn</option>
          <option value="info">Info</option>
          <option value="debug">Debug</option>
        </select>
        <button
          type="button"
          onClick={() => refetch()}
          className="rounded-md border border-zinc-700 px-4 py-2 text-sm text-zinc-300 hover:bg-zinc-800"
        >
          Refresh
        </button>
      </div>

      <div className="flex items-center gap-2 text-xs text-zinc-500">
        <span className="inline-block h-2 w-2 animate-pulse rounded-full bg-green-500" />
        Live stream connected
        {liveLogs.length > 0 && <span>· {liveLogs.length} live entries</span>}
      </div>

      <div
        ref={parentRef}
        className="flex-1 overflow-auto rounded-lg border border-zinc-800 bg-zinc-900 font-mono text-xs"
      >
        {isLoading && logs.length === 0 ? (
          <p className="p-4 text-zinc-500">Loading logs...</p>
        ) : logs.length === 0 ? (
          <p className="p-4 text-zinc-500">No logs found.</p>
        ) : (
          <div style={{ height: `${rowVirtualizer.getTotalSize()}px`, position: 'relative' }}>
            {rowVirtualizer.getVirtualItems().map((virtualRow) => {
              const log = logs[virtualRow.index];
              return (
                <div
                  key={log.id + virtualRow.index}
                  style={{
                    position: 'absolute',
                    top: 0,
                    left: 0,
                    width: '100%',
                    height: `${virtualRow.size}px`,
                    transform: `translateY(${virtualRow.start}px)`,
                  }}
                  className="flex items-center gap-3 border-b border-zinc-800/50 px-3"
                >
                  <span className="shrink-0 text-zinc-600">
                    {new Date(log.timestamp).toLocaleTimeString()}
                  </span>
                  <span
                    className={`w-12 shrink-0 uppercase ${levelColors[log.level.toLowerCase()] ?? 'text-zinc-400'}`}
                  >
                    {log.level}
                  </span>
                  <span className="w-24 shrink-0 truncate text-sentinel-500">{log.service}</span>
                  <span className="truncate text-zinc-300">{log.message}</span>
                </div>
              );
            })}
          </div>
        )}
      </div>
    </div>
  );
}
