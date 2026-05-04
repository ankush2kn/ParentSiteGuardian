import { useCallback, useState } from 'react'
import { changePin, setupPin } from '@/lib/api'
import { useAppState } from '@/context/appStateContext'

export function SettingsPage() {
  const { bootstrap, bootstrapError, reloadBootstrap, clearPinSession, selectedDevice } = useAppState()
  const [setupPinValue, setSetupPinValue] = useState('')
  const [setupConfirm, setSetupConfirm] = useState('')
  const [currentPin, setCurrentPin] = useState('')
  const [newPin, setNewPin] = useState('')
  const [newPin2, setNewPin2] = useState('')
  const [message, setMessage] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

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

  if (bootstrapError) {
    return <p className="page-lead muted">Connect the service to change settings.</p>
  }

  const pinConfigured = bootstrap?.pinConfigured ?? false

  return (
    <div>
      <h1 className="page-title">Settings</h1>
      <p className="page-lead">Parent PIN and device identity for this machine.</p>

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
        <h2 className="card-title">Device</h2>
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
                    <dt>Machine fingerprint</dt>
                    <dd className="mono wrap">{selectedDevice.machineFingerprint}</dd>
                  </>
                ) : null}
                {selectedDevice.windowsUserSid ? (
                  <>
                    <dt>Windows user SID</dt>
                    <dd className="mono wrap">{selectedDevice.windowsUserSid}</dd>
                  </>
                ) : null}
              </dl>
            )}
            <button type="button" className="btn secondary" onClick={() => void copyId()}>
              Copy device ID
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
