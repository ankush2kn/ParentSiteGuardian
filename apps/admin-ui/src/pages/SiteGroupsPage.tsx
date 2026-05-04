import { useCallback, useEffect, useState } from 'react'
import {
  addSiteRule,
  ApiError,
  createSiteGroup,
  deleteSiteGroup,
  deleteSiteRule,
  fetchSiteGroups,
  updateSiteGroup,
} from '@/lib/api'
import type { AdminSiteGroup } from '@/lib/types'
import { useAppState } from '@/context/appStateContext'

const PATTERN_TYPES = [
  { value: 'domain', label: 'Domain (e.g. youtube.com)' },
  { value: 'pathPrefix', label: 'Path prefix (e.g. bing.com/videos)' },
] as const

export function SiteGroupsPage() {
  const { deviceId, bootstrapError, pinConfigured, getPinForChanges } = useAppState()
  const [groups, setGroups] = useState<AdminSiteGroup[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [newName, setNewName] = useState('')

  const load = useCallback(async () => {
    if (!deviceId || bootstrapError) {
      setGroups([])
      setLoading(false)
      return
    }
    setLoading(true)
    setError(null)
    try {
      const res = await fetchSiteGroups(deviceId)
      setGroups(res.groups)
    } catch {
      setError('Failed to load site groups.')
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

  const runWithPin = async (fn: (pin: string) => Promise<void>) => {
    if (!pinConfigured) {
      setError('Set a parent PIN in Settings before changing site groups.')
      return
    }
    const pin = await getPinForChanges()
    if (!pin) return
    try {
      await fn(pin)
      await load()
      setError(null)
    } catch (e) {
      if (e instanceof ApiError && e.status === 401) {
        setError('PIN was rejected. Try again from Settings or re-unlock.')
      } else if (e instanceof ApiError && e.status === 400) {
        setError('Invalid rule pattern or type. Check examples and try again.')
      } else {
        setError('Something went wrong. Check the service log.')
      }
    }
  }

  const onCreateGroup = () => {
    const name = newName.trim()
    if (!name || !deviceId) return
    void runWithPin(async (pin) => {
      await createSiteGroup(deviceId, pin, name)
      setNewName('')
    })
  }

  if (bootstrapError) {
    return <p className="page-lead muted">Connect the service to manage site groups.</p>
  }

  return (
    <div>
      <h1 className="page-title">Site groups</h1>
      <p className="page-lead">
        Groups match URLs for this child profile. Rules are checked in order; the first match wins.
      </p>

      {error ? (
        <p className="banner error" role="alert">
          {error}
        </p>
      ) : null}

      <section className="card">
        <h2 className="card-title">New group</h2>
        <div className="row">
          <input
            className="text-input grow"
            placeholder="Group name (e.g. Social)"
            value={newName}
            onChange={(e) => setNewName(e.target.value)}
          />
          <button type="button" className="btn primary" onClick={onCreateGroup} disabled={!deviceId}>
            Add group
          </button>
        </div>
        {!pinConfigured ? (
          <p className="muted small-gap">Configure a parent PIN under Settings to enable changes.</p>
        ) : null}
      </section>

      {loading ? <p className="muted">Loading…</p> : null}

      {!loading &&
        groups.map((g) => (
          <SiteGroupCard
            key={`${g.id}|${g.name}`}
            group={g}
            deviceId={deviceId}
            pinConfigured={pinConfigured}
            onReload={() => void load()}
            runWithPin={runWithPin}
          />
        ))}
    </div>
  )
}

function SiteGroupCard({
  group,
  deviceId,
  pinConfigured,
  onReload,
  runWithPin,
}: {
  group: AdminSiteGroup
  deviceId: string
  pinConfigured: boolean
  onReload: () => void
  runWithPin: (fn: (pin: string) => Promise<void>) => Promise<void>
}) {
  const [name, setName] = useState(group.name)
  const [pattern, setPattern] = useState('')
  const [patternType, setPatternType] = useState<string>('domain')

  const saveName = () => {
    const n = name.trim()
    if (!n || n === group.name) return
    void runWithPin(async (pin) => {
      await updateSiteGroup(group.id, deviceId, pin, n)
      onReload()
    })
  }

  const removeGroup = () => {
    if (!window.confirm(`Delete group “${group.name}” and all of its rules and limits?`)) return
    void runWithPin(async (pin) => {
      await deleteSiteGroup(group.id, deviceId, pin)
      onReload()
    })
  }

  const addRule = () => {
    const p = pattern.trim()
    if (!p) return
    void runWithPin(async (pin) => {
      await addSiteRule(group.id, deviceId, pin, p, patternType)
      setPattern('')
      onReload()
    })
  }

  const removeRule = (ruleId: string) => {
    void runWithPin(async (pin) => {
      await deleteSiteRule(ruleId, deviceId, pin)
      onReload()
    })
  }

  return (
    <section className="card">
      <div className="card-head">
        <h2 className="card-title">Group</h2>
        <button
          type="button"
          className="btn danger ghost"
          onClick={removeGroup}
          disabled={!pinConfigured}
        >
          Delete group
        </button>
      </div>
      <div className="row">
        <input className="text-input grow" value={name} onChange={(e) => setName(e.target.value)} />
        <button type="button" className="btn secondary" onClick={saveName} disabled={!pinConfigured}>
          Save name
        </button>
      </div>
      <p className="mono small-gap">ID: {group.id}</p>

      <h3 className="subheading">Rules</h3>
      <ul className="rule-list">
        {group.rules.map((r) => (
          <li key={r.id} className="rule-row">
            <code>
              {r.patternType}: {r.pattern}
            </code>
            <button
              type="button"
              className="btn link"
              onClick={() => removeRule(r.id)}
              disabled={!pinConfigured}
            >
              Remove
            </button>
          </li>
        ))}
      </ul>
      <div className="row wrap">
        <select
          className="select-input"
          value={patternType}
          onChange={(e) => setPatternType(e.target.value)}
          disabled={!pinConfigured}
        >
          {PATTERN_TYPES.map((o) => (
            <option key={o.value} value={o.value}>
              {o.label}
            </option>
          ))}
        </select>
        <input
          className="text-input grow"
          placeholder="youtube.com or bing.com/videos"
          value={pattern}
          onChange={(e) => setPattern(e.target.value)}
          disabled={!pinConfigured}
        />
        <button type="button" className="btn primary" onClick={addRule} disabled={!pinConfigured}>
          Add rule
        </button>
      </div>
    </section>
  )
}
