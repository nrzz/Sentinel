import { NavLink } from 'react-router-dom';

const navItems = [
  { to: '/', label: 'Overview', icon: '◉' },
  { to: '/logs', label: 'Logs', icon: '≡' },
  { to: '/metrics', label: 'Metrics', icon: '↗' },
  { to: '/traces', label: 'Traces', icon: '⤳' },
  { to: '/alerts', label: 'Alerts', icon: '⚠' },
  { to: '/incidents', label: 'Incidents', icon: '◈' },
  { to: '/dashboards', label: 'Dashboards', icon: '▦' },
  { to: '/plugins', label: 'Plugins', icon: '⚙' },
  { to: '/settings', label: 'Settings', icon: '☰' },
] as const;

export function Sidebar() {
  return (
    <aside className="flex w-56 shrink-0 flex-col border-r border-zinc-800 bg-zinc-900">
      <div className="flex h-14 items-center gap-2 border-b border-zinc-800 px-4">
        <span className="text-lg font-semibold text-sentinel-400">Sentinel</span>
      </div>
      <nav className="flex-1 space-y-0.5 p-2">
        {navItems.map((item) => (
          <NavLink
            key={item.to}
            to={item.to}
            end={item.to === '/'}
            className={({ isActive }) =>
              `flex items-center gap-3 rounded-md px-3 py-2 text-sm transition-colors ${
                isActive
                  ? 'bg-sentinel-900/50 text-sentinel-300'
                  : 'text-zinc-400 hover:bg-zinc-800 hover:text-zinc-200'
              }`
            }
          >
            <span className="w-4 text-center text-xs opacity-70">{item.icon}</span>
            {item.label}
          </NavLink>
        ))}
      </nav>
      <div className="border-t border-zinc-800 p-3 text-xs text-zinc-500">
        Ctrl+K command palette
      </div>
    </aside>
  );
}
