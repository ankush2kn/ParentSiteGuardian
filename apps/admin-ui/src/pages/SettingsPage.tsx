import { useCallback, useEffect, useState } from 'react'
import { changePin, fetchEffectiveConfig, setupPin } from '@/lib/api'
import type { EffectiveConfigResponse } from '@/lib/types'
import { useAppState } from '@/context/appStateContext'

export function SettingsPage() {
  const { bootstrap, bootstrapError, reloadBootstrap, clearPinSession, selectedDevice } = useAppState()
  const [effective, setEffective] = useState<EffectiveConfigResponse | null>(null)
  const [effectiveError, setEffectiveError] = useState<string | null>(null)
  const [setupPinValue, setSetupPinValue] = useState('')
  const [setupConfirm, setSetupConfirm] = useState('')
  const [currentPin, setCurrentPin] = useState('')
  const [newPin, setNewPin] = useState('')
  const [newPin2, setNewPin2] = useState('')
  const [message, setMessage] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (bootstrapError) return
    let cancelled = false
    const id = window.setTimeout(() => {
      void (async () => {
        try {
          const data = await fetchEffectiveConfig()
          if (!cancelled) {
            setEffective(data)
            setEffectiveError(null)
          }
        } catch {
          if (!cancelled) {
            setEffective(null)
            setEffectiveError('Could not load this machine’s identity from the service.')
          }
        }
      })()
    }, 0)
    return () => {
      cancelled = true
      window.clearTimeout(id)
    }
  }, [bootstrapError])

  const onSetup = async () => {
    setError(null)
    setMessage(null)
    if (setupPinValue.length < 4) {
      setError('PIN must be at least 4 characters.')
      return
    }
    if (setupPinValue !== setupConfirm) {
      setError('PINs do not match.')
      return
    }
    try {
      await setupPin(setupPinValue)
      setSetupPinValue('')
      setSetupConfirm('')
      setMessage('Parent PIN saved.')
      await reloadBootstrap()
    } catch {
      setError('Could not save PIN. It may already be configured — use Change PIN instead.')
    }
  }

  const onChange = async () => {
    setError(null)
    setMessage(null)
    if (newPin.length < 4) {
      setError('New PIN must be at least 4 characters.')
      return
    }
    if (newPin !== newPin2) {
      setError('New PINs do not match.')
      return
    }
    try {
      await changePin(currentPin, newPin)
      setCurrentPin('')
      setNewPin('')
      setNewPin2('')
      clearPinSession()
      setMessage('PIN updated. Unlock again before making other changes.')
    } catch {
      setError('Current PIN was not accepted, or the new PIN was invalid.')
    }
  }

  const copyId = useCallback(async () => {
    if (!selectedDevice?.id) return
    try {
      await navigator.clipboard.writeText(selectedDevice.id)
      setMessage('Device ID copied.')
      setError(null)
    } catch {
      setError('Clipboard not available.')
    }
  }, [selectedDevice])

  const copyAppDeviceId = useCallback(async () => {
    if (!effective?.localDevice.appDeviceId) return
    try {
      await navigator.clipboard.writeText(effective.localDevice.appDeviceId)
      setMessage('App device ID copied.')
      setError(null)
    } catch {
      setError('Clipboard not available.')
    }
  }, [effective])

  if (bootstrapError) {
    return <p className="page-lead muted">Connect the service to change settings.</p>
  }

  const pinConfigured = bootstrap?.pinConfigured ?? false
  const ld = effective?.localDevice
  const fp = ld?.machineFingerprint

  return (
    <div>
      <h1 className="page-title">Settings</h1>
      <p className="page-lead">Parent PIN, this installation’s identity, and the profile device from the database.</p>

      {message ? (
        <p className="banner ok" role="status">
          {message}
        </p>
      ) : null}
      {error ? (
        <p className="banner error" role="alert">
          {error}
        </p>
      ) : null}

      <section className="card">
        <h2 className="card-title">This machine (guardian service)</h2>
        <p className="muted small-gap">
          Stable ID is assigned on first run and stored in SQLite. It is not derived from MAC addresses.
        </p>
        {effectiveError ? <p className="field-error">{effectiveError}</p> : null}
        {ld ? (
          <>
            <p className="mono wrap">
              <strong>App device ID:</strong> {ld.appDeviceId}
            </p>
            <button type="button" className="btn secondary small-gap" onClick={() => void copyAppDeviceId()}>
              Copy app device ID
            </button>
            {effective ? (
              <p className="muted small-gap">
                Extension tick interval (same as <code>GET /api/extension-config</code>):{' '}
                {effective.extension.usageTickIntervalSeconds}s
              </p>
            ) : null}
            {(ld.windowsUsername || ld.windowsUserSid) && (
              <dl className="kv">
                {ld.windowsUsername ? (
                  <>
                    <dt>Windows user</dt>
                    <dd className="mono wrap">{ld.windowsUsername}</dd>
                  </>
                ) : null}
                {ld.windowsUserSid ? (
                  <>
                    <dt>Windows user SID</dt>
                    <dd className="mono wrap">{ld.windowsUserSid}</dd>
                  </>
                ) : null}
              </dl>
            )}
            {fp ? (
              <>
                <h3 className="subheading">Machine fingerprint</h3>
                <dl className="kv">
                  <dt>Machine name</dt>
                  <dd>{fp.machineName}</dd>
                  <dt>OS</dt>
                  <dd className="wrap">{fp.osDescription}</dd>
                  <dt>Architecture</dt>
                  <dd>
                    {fp.osArchitecture} / process: {fp.processArchitecture}
                  </dd>
                  <dt>Processors</dt>
                  <dd>{fp.processorCount}</dd>
                  <dt>Runtime</dt>
                  <dd className="wrap">{fp.frameworkDescription}</dd>
                  <dt>System directory</dt>
                  <dd className="mono wrap">{fp.systemDirectory}</dd>
                  <dt>Time zone</dt>
                  <dd>{fp.localTimeZoneId}</dd>
                  <dt>Captured (UTC)</dt>
                  <dd className="mono">{fp.capturedAtUtc}</dd>
                </dl>
              </>
            ) : null}
            {ld.optionalNetworkLabels.length > 0 ? (
              <>
                <h3 className="subheading">Optional network labels (not used as ID)</h3>
                <p className="muted small-gap">Physical addresses are shown for context only.</p>
                <ul className="rule-list">
                  {ld.optionalNetworkLabels.map((n) => (
                    <li key={`${n.interfaceName}-${n.physicalAddress}`} className="rule-row">
                      <span>
                        <strong>{n.interfaceName}</strong> — {n.description}
                        <span className="mono"> · {n.physicalAddress}</span>
                      </span>
                    </li>
                  ))}
                </ul>
              </>
            ) : (
              <p className="muted small-gap">No non-loopback adapters with a physical address were listed.</p>
            )}
          </>
        ) : !effectiveError ? (
          <p className="muted">Loading…</p>
        ) : null}
      </section>

      <section className="card">
        <h2 className="card-title">Profile device (database)</h2>
        <p className="muted small-gap">Child profile device used for limits and usage — from seed or your data.</p>
        {selectedDevice ? (
          <>
            <p>
              <strong>{selectedDevice.name}</strong>
            </p>
            <p className="mono wrap">ID: {selectedDevice.id}</p>
            <p className="muted small-gap">Created: {new Date(selectedDevice.createdAt).toLocaleString()}</p>
            {(selectedDevice.machineFingerprint || selectedDevice.windowsUserSid) && (
              <dl className="kv">
                {selectedDevice.machineFingerprint ? (
                  <>
                    <dt>Stored fingerprint (DB)</dt>
                    <dd className="mono wrap">{selectedDevice.machineFingerprint}</dd>
                  </>
                ) : null}
                {selectedDevice.windowsUserSid ? (
                  <>
                    <dt>Stored Windows user SID (DB)</dt>
                    <dd className="mono wrap">{selectedDevice.windowsUserSid}</dd>
                  </>
                ) : null}
              </dl>
            )}
            <button type="button" className="btn secondary" onClick={() => void copyId()}>
              Copy profile device ID
            </button>
          </>
        ) : (
          <p className="muted">No device selected.</p>
        )}
      </section>

      {!pinConfigured ? (
        <section className="card">
          <h2 className="card-title">Set parent PIN</h2>
          <p className="muted">Choose a PIN used to confirm changes in this admin UI and on the block page.</p>
          <label className="field-label" htmlFor="setup-pin">
            New PIN
          </label>
          <input
            id="setup-pin"
            className="text-input block"
            type="password"
            autoComplete="new-password"
            value={setupPinValue}
            onChange={(e) => setSetupPinValue(e.target.value)}
          />
          <label className="field-label" htmlFor="setup-pin2">
            Confirm PIN
          </label>
          <input
            id="setup-pin2"
            className="text-input block"
            type="password"
            autoComplete="new-password"
            value={setupConfirm}
            onChange={(e) => setSetupConfirm(e.target.value)}
          />
          <button type="button" className="btn primary" onClick={() => void onSetup()}>
            Save PIN
          </button>
        </section>
      ) : (
        <section className="card">
          <h2 className="card-title">Change parent PIN</h2>
          <label className="field-label" htmlFor="cur-pin">
            Current PIN
          </label>
          <input
            id="cur-pin"
            className="text-input block"
            type="password"
            autoComplete="current-password"
            value={currentPin}
            onChange={(e) => setCurrentPin(e.target.value)}
          />
          <label className="field-label" htmlFor="new-pin">
            New PIN
          </label>
          <input
            id="new-pin"
            className="text-input block"
            type="password"
            autoComplete="new-password"
            value={newPin}
            onChange={(e) => setNewPin(e.target.value)}
          />
          <label className="field-label" htmlFor="new-pin2">
            Confirm new PIN
          </label>
          <input
            id="new-pin2"
            className="text-input block"
            type="password"
            autoComplete="new-password"
            value={newPin2}
            onChange={(e) => setNewPin2(e.target.value)}
          />
          <button type="button" className="btn primary" onClick={() => void onChange()}>
            Update PIN
          </button>
        </section>
      )}

      {pinConfigured ? (
        <section className="card">
          <h2 className="card-title">Admin session</h2>
          <p className="muted">Clear the unlocked PIN session (for example after you finish making changes).</p>
          <button type="button" className="btn secondary" onClick={clearPinSession}>
            Lock admin
          </button>
        </section>
      ) : null}
    </div>
  )
}
