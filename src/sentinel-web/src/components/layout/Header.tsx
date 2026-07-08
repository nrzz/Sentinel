import { useAuthStore } from '@/stores/authStore';

interface HeaderProps {
  title: string;
  onOpenCommandPalette: () => void;
}

export function Header({ title, onOpenCommandPalette }: HeaderProps) {
  const { user, logout } = useAuthStore();

  return (
    <header className="flex h-14 items-center justify-between border-b border-zinc-800 bg-zinc-950 px-6">
      <h1 className="text-lg font-medium text-zinc-100">{title}</h1>
      <div className="flex items-center gap-4">
        <button
          type="button"
          onClick={onOpenCommandPalette}
          className="rounded-md border border-zinc-700 bg-zinc-900 px-3 py-1.5 text-xs text-zinc-400 hover:border-zinc-600 hover:text-zinc-200"
        >
          <kbd className="font-mono">Ctrl</kbd>+<kbd className="font-mono">K</kbd>
        </button>
        {user && (
          <div className="flex items-center gap-3">
            <span className="text-sm text-zinc-400">{user.displayName}</span>
            <button
              type="button"
              onClick={logout}
              className="rounded-md px-2 py-1 text-xs text-zinc-500 hover:bg-zinc-800 hover:text-zinc-300"
            >
              Sign out
            </button>
          </div>
        )}
      </div>
    </header>
  );
}
