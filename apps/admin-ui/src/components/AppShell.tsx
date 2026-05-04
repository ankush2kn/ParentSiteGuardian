import { NavLink, Outlet } from 'react-router-dom'
import { useAppState } from '@/context/appStateContext'

const nav = [
  { to: '/dashboard', label: 'Dashboard' },
  { to: '/site-groups', label: 'Site groups' },
  { to: '/limits', label: 'Limits' },
  { to: '/history', label: 'History' },
  { to: '/settings', label: 'Settings' },
] as const

export function AppShell() {
  const { bootstrap, bootstrapError, deviceId, setDeviceId, clearPinSession } = useAppState()

  return (
    <div className="shell">
      <header className="shell-header">
        <div className="shell-brand">
          <span className="shell-title">Parent Site Guardian</span>
          <span className="shell-sub">Local admin</span>
        </div>
        <div className="shell-controls">
          <label className="device-label" htmlFor="device-select">
            Device
          </label>
          <select
            id="device-select"
            className="select-input"
            value={deviceId}
            onChange={(e) => {
              clearPinSession()
              setDeviceId(e.target.value)
            }}
            disabled={!bootstrap?.devices.length}
          >
            {(bootstrap?.devices ?? []).map((d) => (
              <option key={d.id} value={d.id}>
                {d.name}
              </option>
            ))}
          </select>
        </div>
      </header>

      {bootstrapError ? (
        <div className="banner error" role="alert">
          {bootstrapError}
        </div>
      ) : null}

      <div className="shell-body">
        <nav className="side-nav" aria-label="Main">
          {nav.map(({ to, label }) => (
            <NavLink
              key={to}
              to={to}
              className={({ isActive }) => `nav-link${isActive ? ' active' : ''}`}
            >
              {label}
            </NavLink>
          ))}
        </nav>
        <main className="main-panel">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
