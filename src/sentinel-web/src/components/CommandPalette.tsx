import { useEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';

interface Command {
  id: string;
  label: string;
  shortcut?: string;
  action: () => void;
}

interface CommandPaletteProps {
  open: boolean;
  onClose: () => void;
}

export function CommandPalette({ open, onClose }: CommandPaletteProps) {
  const navigate = useNavigate();
  const [query, setQuery] = useState('');
  const [selectedIndex, setSelectedIndex] = useState(0);
  const inputRef = useRef<HTMLInputElement>(null);

  const commands: Command[] = [
    { id: 'overview', label: 'Go to Overview', action: () => navigate('/') },
    { id: 'logs', label: 'Go to Logs', action: () => navigate('/logs') },
    { id: 'metrics', label: 'Go to Metrics', action: () => navigate('/metrics') },
    { id: 'traces', label: 'Go to Traces', action: () => navigate('/traces') },
    { id: 'alerts', label: 'Go to Alerts', action: () => navigate('/alerts') },
    { id: 'incidents', label: 'Go to Incidents', action: () => navigate('/incidents') },
    { id: 'dashboards', label: 'Go to Dashboards', action: () => navigate('/dashboards') },
    { id: 'plugins', label: 'Go to Plugins', action: () => navigate('/plugins') },
    { id: 'settings', label: 'Go to Settings', action: () => navigate('/settings') },
  ];

  const filtered = commands.filter((cmd) =>
    cmd.label.toLowerCase().includes(query.toLowerCase()),
  );

  useEffect(() => {
    if (open) {
      setQuery('');
      setSelectedIndex(0);
      requestAnimationFrame(() => inputRef.current?.focus());
    }
  }, [open]);

  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
        e.preventDefault();
        if (open) {
          onClose();
        }
      }
    };
    window.addEventListener('keydown', handler);
    return () => window.removeEventListener('keydown', handler);
  }, [open, onClose]);

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Escape') {
      onClose();
    } else if (e.key === 'ArrowDown') {
      e.preventDefault();
      setSelectedIndex((i) => Math.min(i + 1, filtered.length - 1));
    } else if (e.key === 'ArrowUp') {
      e.preventDefault();
      setSelectedIndex((i) => Math.max(i - 1, 0));
    } else if (e.key === 'Enter' && filtered[selectedIndex]) {
      filtered[selectedIndex].action();
      onClose();
    }
  };

  if (!open) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-start justify-center bg-black/60 pt-[20vh]">
      <div
        className="w-full max-w-lg overflow-hidden rounded-lg border border-zinc-700 bg-zinc-900 shadow-2xl"
        role="dialog"
        aria-label="Command palette"
      >
        <input
          ref={inputRef}
          type="text"
          value={query}
          onChange={(e) => {
            setQuery(e.target.value);
            setSelectedIndex(0);
          }}
          onKeyDown={handleKeyDown}
          placeholder="Type a command..."
          className="w-full border-b border-zinc-700 bg-transparent px-4 py-3 text-sm text-zinc-100 outline-none placeholder:text-zinc-500"
        />
        <ul className="max-h-64 overflow-auto py-1">
          {filtered.map((cmd, index) => (
            <li key={cmd.id}>
              <button
                type="button"
                onClick={() => {
                  cmd.action();
                  onClose();
                }}
                className={`flex w-full items-center px-4 py-2 text-left text-sm ${
                  index === selectedIndex
                    ? 'bg-sentinel-900/40 text-sentinel-300'
                    : 'text-zinc-300 hover:bg-zinc-800'
                }`}
              >
                {cmd.label}
              </button>
            </li>
          ))}
          {filtered.length === 0 && (
            <li className="px-4 py-3 text-sm text-zinc-500">No commands found</li>
          )}
        </ul>
      </div>
      <button
        type="button"
        className="fixed inset-0 -z-10"
        aria-label="Close command palette"
        onClick={onClose}
      />
    </div>
  );
}
