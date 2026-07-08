import { useState } from 'react';

export function SettingsPage() {
  const [apiUrl, setApiUrl] = useState('/api/v1');
  const [refreshInterval, setRefreshInterval] = useState(30);
  const [saved, setSaved] = useState(false);

  const handleSave = (e: React.FormEvent) => {
    e.preventDefault();
    setSaved(true);
    setTimeout(() => setSaved(false), 2000);
  };

  return (
    <div className="mx-auto max-w-xl space-y-6">
      <p className="text-sm text-zinc-400">Configure your Sentinel workspace preferences.</p>

      <form onSubmit={handleSave} className="space-y-6 rounded-lg border border-zinc-800 bg-zinc-900 p-6">
        <div>
          <label htmlFor="apiUrl" className="mb-1 block text-sm text-zinc-300">
            API Base URL
          </label>
          <input
            id="apiUrl"
            type="text"
            value={apiUrl}
            onChange={(e) => setApiUrl(e.target.value)}
            className="w-full rounded-md border border-zinc-700 bg-zinc-950 px-3 py-2 text-sm text-zinc-100"
          />
          <p className="mt-1 text-xs text-zinc-600">
            Override only when not using the Vite dev proxy.
          </p>
        </div>

        <div>
          <label htmlFor="refresh" className="mb-1 block text-sm text-zinc-300">
            Auto-refresh interval (seconds)
          </label>
          <input
            id="refresh"
            type="number"
            min={5}
            max={300}
            value={refreshInterval}
            onChange={(e) => setRefreshInterval(Number(e.target.value))}
            className="w-full rounded-md border border-zinc-700 bg-zinc-950 px-3 py-2 text-sm text-zinc-100"
          />
        </div>

        <div className="flex items-center gap-3">
          <button
            type="submit"
            className="rounded-md bg-sentinel-600 px-4 py-2 text-sm text-white hover:bg-sentinel-500"
          >
            Save preferences
          </button>
          {saved && <span className="text-sm text-green-400">Saved</span>}
        </div>
      </form>

      <div className="rounded-lg border border-zinc-800 bg-zinc-900 p-6">
        <h2 className="text-sm font-medium text-zinc-300">About</h2>
        <dl className="mt-3 space-y-2 text-sm">
          <div className="flex justify-between">
            <dt className="text-zinc-500">Version</dt>
            <dd className="text-zinc-300">0.1.0</dd>
          </div>
          <div className="flex justify-between">
            <dt className="text-zinc-500">Environment</dt>
            <dd className="text-zinc-300">development</dd>
          </div>
        </dl>
      </div>
    </div>
  );
}
