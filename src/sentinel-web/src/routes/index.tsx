import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { AppShell } from '@/components/layout/AppShell';
import { ProtectedRoute } from '@/hooks/useAuth';
import { LoginPage } from '@/pages/LoginPage';
import { OverviewPage } from '@/pages/OverviewPage';
import { LogsPage } from '@/pages/LogsPage';
import { MetricsPage } from '@/pages/MetricsPage';
import { TracesPage } from '@/pages/TracesPage';
import { AlertsPage } from '@/pages/AlertsPage';
import { IncidentsPage } from '@/pages/IncidentsPage';
import { DashboardsPage } from '@/pages/DashboardsPage';
import { PluginsPage } from '@/pages/PluginsPage';
import { SettingsPage } from '@/pages/SettingsPage';

export function AppRoutes() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route
          element={
            <ProtectedRoute>
              <AppShell />
            </ProtectedRoute>
          }
        >
          <Route index element={<OverviewPage />} />
          <Route path="logs" element={<LogsPage />} />
          <Route path="metrics" element={<MetricsPage />} />
          <Route path="traces" element={<TracesPage />} />
          <Route path="alerts" element={<AlertsPage />} />
          <Route path="incidents" element={<IncidentsPage />} />
          <Route path="dashboards" element={<DashboardsPage />} />
          <Route path="plugins" element={<PluginsPage />} />
          <Route path="settings" element={<SettingsPage />} />
        </Route>
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  );
}
