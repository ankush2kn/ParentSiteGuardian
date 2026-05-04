import { useId, useState } from 'react'

type PinUnlockModalProps = {
  open: boolean
  errorText: string | null
  onClose: () => void
  onSubmit: (pin: string) => void | Promise<void>
}

export function PinUnlockModal({ open, errorText, onClose, onSubmit }: PinUnlockModalProps) {
  const labelId = useId()
  const [pin, setPin] = useState('')
  const [busy, setBusy] = useState(false)

  if (!open) return null

  const submit = async () => {
    if (!pin.trim()) return
    setBusy(true)
    try {
      await onSubmit(pin)
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="modal-backdrop" role="presentation" onMouseDown={(e) => e.target === e.currentTarget && onClose()}>
      <div
        className="modal"
        role="dialog"
        aria-modal="true"
        aria-labelledby={labelId}
        onMouseDown={(e) => e.stopPropagation()}
      >
        <h2 id={labelId} className="modal-title">
          Parent PIN required
        </h2>
        <p className="modal-desc">Enter your PIN to save changes. Your session stays unlocked for 30 minutes.</p>
        <label className="field-label" htmlFor="unlock-pin">
          PIN
        </label>
        <input
          id="unlock-pin"
          className="text-input"
          type="password"
          inputMode="numeric"
          autoComplete="current-password"
          value={pin}
          onChange={(e) => setPin(e.target.value)}
          onKeyDown={(e) => e.key === 'Enter' && void submit()}
        />
        {errorText ? <p className="field-error">{errorText}</p> : null}
        <div className="modal-actions">
          <button type="button" className="btn secondary" onClick={onClose} disabled={busy}>
            Cancel
          </button>
          <button type="button" className="btn primary" onClick={() => void submit()} disabled={busy}>
            {busy ? 'Checking…' : 'Unlock'}
          </button>
        </div>
      </div>
    </div>
  )
}
