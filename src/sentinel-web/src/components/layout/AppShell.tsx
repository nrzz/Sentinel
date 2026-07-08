import { useState } from 'react';
import { Outlet, useLocation } from 'react-router-dom';
import { Sidebar } from './Sidebar';
import { Header } from './Header';
import { CommandPalette } from '@/components/CommandPalette';

const pageTitles: Record<string, string> = {
  '/': 'Overview',
  '/logs': 'Logs',
  '/metrics': 'Metrics',
  '/traces': 'Traces',
  '/alerts': 'Alerts',
  '/incidents': 'Incidents',
  '/dashboards': 'Dashboards',
  '/plugins': 'Plugins',
  '/settings': 'Settings',
};

export function AppShell() {
  const location = useLocation();
  const [paletteOpen, setPaletteOpen] = useState(false);
  const title = pageTitles[location.pathname] ?? 'Sentinel';

  return (
    <div className="flex h-full">
      <Sidebar />
      <div className="flex min-w-0 flex-1 flex-col">
        <Header title={title} onOpenCommandPalette={() => setPaletteOpen(true)} />
        <main className="flex-1 overflow-auto p-6">
          <Outlet />
        </main>
      </div>
      <CommandPalette open={paletteOpen} onClose={() => setPaletteOpen(false)} />
    </div>
  );
}
