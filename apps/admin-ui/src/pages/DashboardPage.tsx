import { useCallback, useEffect, useState } from 'react'
import { fetchTodayUsage } from '@/lib/api'
import { formatDuration } from '@/lib/format'
import type { TodayUsageResponse } from '@/lib/types'
import { useAppState } from '@/context/appStateContext'

export function DashboardPage() {
  const { deviceId, bootstrapError } = useAppState()
  const [data, setData] = useState<TodayUsageResponse | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)

  const load = useCallback(async () => {
    if (!deviceId || bootstrapError) {
      setLoading(false)
      setData(null)
      return
    }
    setLoading(true)
    setError(null)
    try {
      setData(await fetchTodayUsage(deviceId))
    } catch {
      setError('Could not load usage for this device.')
      setData(null)
    } finally {
      setLoading(false)
    }
  }, [deviceId, bootstrapError])

  useEffect(() => {
    const id = window.setTimeout(() => {
      void load()
    }, 0)
    return () => window.clearTimeout(id)
  }, [load])

  if (bootstrapError) {
    return <p className="page-lead muted">Connect the service to see the dashboard.</p>
  }

  if (!deviceId) {
    return <p className="page-lead muted">No device registered yet.</p>
  }

  return (
    <div>
      <h1 className="page-title">Dashboard</h1>
      <p className="page-lead">
        Today ({data?.localDate ?? '—'}) · limits reset at local midnight.
      </p>

      {loading ? <p className="muted">Loading…</p> : null}
      {error ? (
        <p className="banner error" role="alert">
          {error}
        </p>
      ) : null}

      {!loading && data && data.groups.length === 0 ? (
        <p className="muted">No site groups with daily limits for this device. Add limits under Limits.</p>
      ) : null}

      {!loading && data && data.groups.length > 0 ? (
        <div className="table-wrap">
          <table className="data-table">
            <thead>
              <tr>
                <th>Site group</th>
                <th>Used</th>
                <th>Daily cap</th>
                <th>Remaining</th>
                <th>Status</th>
              </tr>
            </thead>
            <tbody>
              {data.groups.map((g) => {
                const exhausted = g.remainingSeconds <= 0
                return (
                  <tr key={g.siteGroupId}>
                    <td>{g.siteGroupName}</td>
                    <td>{formatDuration(g.usedSeconds)}</td>
                    <td>{formatDuration(g.effectiveLimitSeconds)}</td>
                    <td>{formatDuration(g.remainingSeconds)}</td>
                    <td>
                      <span className={`badge ${exhausted ? 'bad' : 'ok'}`}>
                        {exhausted ? 'Limit reached' : 'Within limit'}
                      </span>
                      {g.overrideBonusSeconds > 0 ? (
                        <span className="muted small-block">
                          Includes {formatDuration(g.overrideBonusSeconds)} bonus from overrides
                        </span>
                      ) : null}
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      ) : null}
    </div>
  )
}
