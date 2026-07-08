import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { configureApiClient } from '@/lib/api/client';
import { useAuthStore } from '@/stores/authStore';
import { AppRoutes } from '@/routes';
import './index.css';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      retry: 1,
    },
  },
});

configureApiClient(
  () => useAuthStore.getState().getAccessToken(),
  () => useAuthStore.getState().refresh(),
  () => useAuthStore.getState().user?.tenantId ?? null,
);

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <AppRoutes />
    </QueryClientProvider>
  </StrictMode>,
);
