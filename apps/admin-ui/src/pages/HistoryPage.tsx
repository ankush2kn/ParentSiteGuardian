import { useCallback, useEffect, useMemo, useState } from 'react'
import { fetchUsageHistory } from '@/lib/api'
import { formatDuration } from '@/lib/format'
import type { UsageHistoryResponse } from '@/lib/types'
import { useAppState } from '@/context/appStateContext'

export function HistoryPage() {
  const { deviceId, bootstrapError } = useAppState()
  const [data, setData] = useState<UsageHistoryResponse | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)

  const load = useCallback(async () => {
    if (!deviceId || bootstrapError) {
      setData(null)
      setLoading(false)
      return
    }
    setLoading(true)
    setError(null)
    try {
      setData(await fetchUsageHistory(deviceId, 30))
    } catch {
      setError('Failed to load history.')
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

  /** Newest local day first for the table body. */
  const displayDays = useMemo(
    () => (data?.days.length ? [...data.days].reverse() : []),
    [data],
  )

  const groupColumns = useMemo(() => displayDays[0]?.groups ?? [], [displayDays])

  if (bootstrapError) {
    return <p className="page-lead muted">Connect the service to view history.</p>
  }

  return (
    <div>
      <h1 className="page-title">History</h1>
      <p className="page-lead">Per-day usage by site group for the last 30 local days.</p>

      {loading ? <p className="muted">Loading…</p> : null}
      {error ? (
        <p className="banner error" role="alert">
          {error}
        </p>
      ) : null}

      {!loading && data && displayDays.length > 0 ? (
        <div className="table-wrap scroll-x">
          <table className="data-table compact">
            <thead>
              <tr>
                <th>Date</th>
                {groupColumns.map((g) => (
                  <th key={g.siteGroupId} className="col-tight" title={g.siteGroupName}>
                    {g.siteGroupName}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {displayDays.map((day) => (
                <tr key={day.localDate}>
                  <td>{day.localDate}</td>
                  {day.groups.map((g) => (
                    <td key={g.siteGroupId}>{formatDuration(g.usedSeconds)}</td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : null}
    </div>
  )
}
