import { useCallback, useEffect, useState } from 'react'
import { fetchDailyLimits, fetchSiteGroups, pinErrorMessage, upsertDailyLimit } from '@/lib/api'
import { formatDuration, minutesToSeconds, secondsToMinutes } from '@/lib/format'
import { useAppState } from '@/context/appStateContext'

type RowState = {
  siteGroupId: string
  siteGroupName: string
  limitSeconds: number
  draftMinutes: string
}

export function LimitsPage() {
  const { deviceId, bootstrapError, pinConfigured, getPinForChanges, clearPinSession } = useAppState()
  const [rows, setRows] = useState<RowState[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [saved, setSaved] = useState<string | null>(null)

  const load = useCallback(async () => {
    if (!deviceId || bootstrapError) {
      setRows([])
      setLoading(false)
      return
    }
    setLoading(true)
    setError(null)
    try {
      const [groupsRes, limits] = await Promise.all([
        fetchSiteGroups(deviceId),
        fetchDailyLimits(deviceId),
      ])
      const limitByGroup = new Map(limits.map((l) => [l.siteGroupId, l.limitSeconds]))
      setRows(
        groupsRes.groups.map((g) => {
          const limitSeconds = limitByGroup.get(g.id) ?? 0
          return {
            siteGroupId: g.id,
            siteGroupName: g.name,
            limitSeconds,
            draftMinutes: String(secondsToMinutes(limitSeconds) || 0),
          }
        }),
      )
    } catch {
      setError('Failed to load limits.')
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

  const updateDraft = useCallback((siteGroupId: string, value: string) => {
    setRows((prev) =>
      prev.map((r) => (r.siteGroupId === siteGroupId ? { ...r, draftMinutes: value } : r)),
    )
  }, [])

  const saveRow = async (siteGroupId: string) => {
    if (!pinConfigured) {
      setError('Set a parent PIN in Settings before changing limits.')
      return
    }
    const row = rows.find((r) => r.siteGroupId === siteGroupId)
    if (!row) return
    const minutes = Number.parseFloat(row.draftMinutes)
    if (!Number.isFinite(minutes) || minutes < 0) {
      setError('Enter a valid number of minutes.')
      return
    }
    const pin = await getPinForChanges()
    if (!pin) return
    const limitSeconds = minutesToSeconds(minutes)
    try {
      await upsertDailyLimit(deviceId, siteGroupId, pin, limitSeconds)
      setError(null)
      setSaved('Saved.')
      window.setTimeout(() => setSaved(null), 2000)
      await load()
    } catch (e) {
      const pinMessage = pinErrorMessage(e)
      if (pinMessage) {
        clearPinSession()
        setError(pinMessage)
      } else {
        setError('Could not save limit.')
      }
    }
  }

  if (bootstrapError) {
    return <p className="page-lead muted">Connect the service to edit limits.</p>
  }

  return (
    <div>
      <h1 className="page-title">Limits</h1>
      <p className="page-lead">Daily screen-time budget per site group on this device (local calendar day).</p>

      {error ? (
        <p className="banner error" role="alert">
          {error}
        </p>
      ) : null}
      {saved ? (
        <p className="banner ok" role="status">
          {saved}
        </p>
      ) : null}

      {loading ? <p className="muted">Loading…</p> : null}

      {!loading && rows.length === 0 ? (
        <p className="muted">No site groups yet. Create groups under Site groups first.</p>
      ) : null}

      {!loading && rows.length > 0 ? (
        <div className="table-wrap">
          <table className="data-table">
            <thead>
              <tr>
                <th>Site group</th>
                <th>Daily limit (minutes)</th>
                <th>Current (saved)</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {rows.map((r) => (
                <tr key={r.siteGroupId}>
                  <td>{r.siteGroupName}</td>
                  <td>
                    <input
                      className="text-input narrow"
                      type="number"
                      min={0}
                      step={1}
                      value={r.draftMinutes}
                      onChange={(e) => updateDraft(r.siteGroupId, e.target.value)}
                      disabled={!pinConfigured}
                    />
                  </td>
                  <td>{formatDuration(r.limitSeconds)}</td>
                  <td>
                    <button
                      type="button"
                      className="btn primary"
                      onClick={() => void saveRow(r.siteGroupId)}
                      disabled={!pinConfigured}
                    >
                      Save
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : null}
    </div>
  )
}
