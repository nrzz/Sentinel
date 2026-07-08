import { useQuery } from '@tanstack/react-query';
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  Legend,
} from 'recharts';
import { queryMetrics } from '@/lib/api/metrics';

export function MetricsPage() {
  const { data: series = [], isLoading } = useQuery({
    queryKey: ['metrics'],
    queryFn: () => queryMetrics({ step: '1m' }),
  });

  const chartData = buildChartData(series);

  return (
    <div className="space-y-6">
      <p className="text-sm text-zinc-400">Time-series metrics from instrumented services.</p>

      {isLoading ? (
        <p className="text-sm text-zinc-500">Loading metrics...</p>
      ) : series.length === 0 ? (
        <div className="rounded-lg border border-zinc-800 bg-zinc-900 p-8 text-center">
          <p className="text-sm text-zinc-500">No metrics data available.</p>
          <p className="mt-1 text-xs text-zinc-600">
            Instrument your services with OpenTelemetry to populate this view.
          </p>
        </div>
      ) : (
        <div className="rounded-lg border border-zinc-800 bg-zinc-900 p-4">
          <ResponsiveContainer width="100%" height={400}>
            <LineChart data={chartData}>
              <CartesianGrid strokeDasharray="3 3" stroke="#3f3f46" />
              <XAxis dataKey="timestamp" stroke="#71717a" tick={{ fontSize: 11 }} />
              <YAxis stroke="#71717a" tick={{ fontSize: 11 }} />
              <Tooltip
                contentStyle={{
                  backgroundColor: '#18181b',
                  border: '1px solid #3f3f46',
                  borderRadius: '6px',
                }}
              />
              <Legend />
              {series.map((s, i) => (
                <Line
                  key={s.name}
                  type="monotone"
                  dataKey={s.name}
                  stroke={CHART_COLORS[i % CHART_COLORS.length]}
                  dot={false}
                  strokeWidth={2}
                />
              ))}
            </LineChart>
          </ResponsiveContainer>
        </div>
      )}
    </div>
  );
}

const CHART_COLORS = ['#2dd4bf', '#60a5fa', '#f472b6', '#fbbf24', '#a78bfa'];

function buildChartData(
  series: Awaited<ReturnType<typeof queryMetrics>>,
): Record<string, string | number>[] {
  if (series.length === 0) return [];

  const timestamps = new Set<string>();
  series.forEach((s) => s.dataPoints.forEach((dp) => timestamps.add(dp.timestamp)));

  return Array.from(timestamps)
    .sort()
    .map((ts) => {
      const point: Record<string, string | number> = {
        timestamp: new Date(ts).toLocaleTimeString(),
      };
      series.forEach((s) => {
        const dp = s.dataPoints.find((d) => d.timestamp === ts);
        point[s.name] = dp?.value ?? 0;
      });
      return point;
    });
}
