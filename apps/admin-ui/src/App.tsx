import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import '@/App.css'
import { AppShell } from '@/components/AppShell'
import { AppStateProvider } from '@/providers/AppStateProvider'
import { DashboardPage } from '@/pages/DashboardPage'
import { HistoryPage } from '@/pages/HistoryPage'
import { LimitsPage } from '@/pages/LimitsPage'
import { SettingsPage } from '@/pages/SettingsPage'
import { SiteGroupsPage } from '@/pages/SiteGroupsPage'

export default function App() {
  return (
    <BrowserRouter>
      <AppStateProvider>
        <Routes>
          <Route element={<AppShell />}>
            <Route path="/dashboard" element={<DashboardPage />} />
            <Route path="/site-groups" element={<SiteGroupsPage />} />
            <Route path="/limits" element={<LimitsPage />} />
            <Route path="/history" element={<HistoryPage />} />
            <Route path="/settings" element={<SettingsPage />} />
          </Route>
          <Route path="/" element={<Navigate to="/dashboard" replace />} />
          <Route path="*" element={<Navigate to="/dashboard" replace />} />
        </Routes>
      </AppStateProvider>
    </BrowserRouter>
  )
}
