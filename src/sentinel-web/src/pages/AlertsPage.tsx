import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  listAlertExecutions,
  listAlertRules,
  silenceAlertExecution,
  type AlertExecution,
  type AlertRule,
} from '@/lib/api/alerts';

const severityStyles: Record<string, string> = {
  critical: 'bg-red-900/40 text-red-300 border-red-800',
  warning: 'bg-yellow-900/40 text-yellow-300 border-yellow-800',
  info: 'bg-blue-900/40 text-blue-300 border-blue-800',
};

const ruleStatusStyles: Record<string, string> = {
  active: 'text-green-400',
  paused: 'text-yellow-400',
  disabled: 'text-zinc-500',
};

function RuleCard({ rule }: { rule: AlertRule }) {
  return (
    <div className="rounded-lg border border-zinc-800 bg-zinc-900 p-4">
      <div className="flex items-center gap-2">
        <span className="font-medium text-zinc-200">{rule.name}</span>
        <span className={`rounded border px-2 py-0.5 text-xs ${severityStyles[rule.severity]}`}>
          {rule.severity}
        </span>
        <span className={`text-xs ${ruleStatusStyles[rule.status]}`}>{rule.status}</span>
      </div>
      <p className="mt-1 text-sm text-zinc-400">{rule.description || rule.query}</p>
      <p className="mt-1 text-xs text-zinc-600">
        Evaluates every {rule.evaluationIntervalSeconds}s
      </p>
    </div>
  );
}

function ExecutionCard({
  execution,
  onSilence,
  isSilencing,
}: {
  execution: AlertExecution;
  onSilence: (id: string) => void;
  isSilencing: boolean;
}) {
  return (
    <div className="flex items-center justify-between rounded-lg border border-zinc-800 bg-zinc-900 p-4">
      <div className="space-y-1">
        <div className="flex items-center gap-2">
          <span
            className={`rounded border px-2 py-0.5 text-xs ${severityStyles[execution.severity]}`}
          >
            {execution.severity}
          </span>
          <span className="text-xs text-zinc-500">{execution.status}</span>
        </div>
        <p className="text-sm text-zinc-400">{execution.message}</p>
        <p className="text-xs text-zinc-600">
          Triggered {new Date(execution.triggeredAt).toLocaleString()}
        </p>
      </div>
      {execution.status === 'triggered' && (
        <button
          type="button"
          onClick={() => onSilence(execution.id)}
          disabled={isSilencing}
          className="rounded-md border border-zinc-700 px-3 py-1.5 text-xs text-zinc-400 hover:bg-zinc-800"
        >
          Silence 1h
        </button>
      )}
    </div>
  );
}

export function AlertsPage() {
  const queryClient = useQueryClient();
  const { data: rules = [], isLoading: rulesLoading } = useQuery({
    queryKey: ['alert-rules'],
    queryFn: listAlertRules,
  });
  const { data: executions = [], isLoading: executionsLoading } = useQuery({
    queryKey: ['alert-executions'],
    queryFn: () => listAlertExecutions(50),
  });

  const silence = useMutation({
    mutationFn: (id: string) => silenceAlertExecution(id, 60),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['alert-executions'] });
    },
  });

  const isLoading = rulesLoading || executionsLoading;

  return (
    <div className="space-y-6">
      <section className="space-y-4">
        <p className="text-sm text-zinc-400">Configured alert rules.</p>
        {isLoading ? (
          <p className="text-sm text-zinc-500">Loading alert rules...</p>
        ) : rules.length === 0 ? (
          <div className="rounded-lg border border-zinc-800 bg-zinc-900 p-8 text-center">
            <p className="text-sm text-zinc-500">No alert rules configured.</p>
          </div>
        ) : (
          <div className="space-y-3">
            {rules.map((rule) => (
              <RuleCard key={rule.id} rule={rule} />
            ))}
          </div>
        )}
      </section>

      <section className="space-y-4">
        <p className="text-sm text-zinc-400">Recent alert executions.</p>
        {executions.length === 0 ? (
          <div className="rounded-lg border border-zinc-800 bg-zinc-900 p-8 text-center">
            <p className="text-sm text-zinc-500">No recent executions.</p>
          </div>
        ) : (
          <div className="space-y-3">
            {executions.map((execution) => (
              <ExecutionCard
                key={execution.id}
                execution={execution}
                onSilence={(id) => silence.mutate(id)}
                isSilencing={silence.isPending}
              />
            ))}
          </div>
        )}
      </section>
    </div>
  );
}
