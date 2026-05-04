import { useCallback, useEffect, useMemo, useRef, useState, type ReactNode } from 'react'
import { PinUnlockModal } from '@/components/PinUnlockModal'
import { AppStateContext, type AppStateContextValue } from '@/context/appStateContext'
import { fetchBootstrap, verifyPin } from '@/lib/api'
import type { AdminBootstrapResponse } from '@/lib/types'

const DEVICE_KEY = 'pg_admin_device_id'
const SESSION_MS = 30 * 60 * 1000

export function AppStateProvider({ children }: { children: ReactNode }) {
  const [bootstrap, setBootstrap] = useState<AdminBootstrapResponse | null>(null)
  const [bootstrapError, setBootstrapError] = useState<string | null>(null)
  const [deviceId, setDeviceIdState] = useState('')
  const [pinSession, setPinSession] = useState<{ pin: string; expiresAt: number } | null>(null)
  const [pinModalOpen, setPinModalOpen] = useState(false)
  const [pinModalKey, setPinModalKey] = useState(0)
  const [pinModalError, setPinModalError] = useState<string | null>(null)
  const pendingPinRef = useRef<((pin: string | null) => void) | null>(null)

  const reloadBootstrap = useCallback(async () => {
    setBootstrapError(null)
    try {
      const data = await fetchBootstrap()
      setBootstrap(data)
      const stored = localStorage.getItem(DEVICE_KEY)
      const first = data.devices[0]?.id ?? ''
      const next =
        stored && data.devices.some((d) => d.id === stored) ? stored : first
      setDeviceIdState(next)
    } catch {
      setBootstrap(null)
      setBootstrapError(
        'Could not reach the guardian service. Start it with npm run dev:service (default http://127.0.0.1:5050).',
      )
    }
  }, [])

  useEffect(() => {
    const id = window.setTimeout(() => {
      void reloadBootstrap()
    }, 0)
    return () => window.clearTimeout(id)
  }, [reloadBootstrap])

  useEffect(() => {
    if (!pinSession) return
    const t = window.setInterval(() => {
      if (Date.now() >= pinSession.expiresAt) setPinSession(null)
    }, 5000)
    return () => window.clearInterval(t)
  }, [pinSession])

  const setDeviceId = useCallback((id: string) => {
    setDeviceIdState(id)
    if (id) localStorage.setItem(DEVICE_KEY, id)
  }, [])

  const selectedDevice = useMemo(
    () => bootstrap?.devices.find((d) => d.id === deviceId),
    [bootstrap, deviceId],
  )

  const pinConfigured = bootstrap?.pinConfigured ?? false

  const getPinForChanges = useCallback((): Promise<string | null> => {
    if (!pinConfigured) {
      return Promise.resolve(null)
    }
    if (pinSession && Date.now() < pinSession.expiresAt) {
      return Promise.resolve(pinSession.pin)
    }
    setPinModalError(null)
    setPinModalKey((k) => k + 1)
    setPinModalOpen(true)
    return new Promise((resolve) => {
      pendingPinRef.current = resolve
    })
  }, [pinConfigured, pinSession])

  const clearPinSession = useCallback(() => {
    setPinSession(null)
  }, [])

  const handlePinModalSubmit = useCallback(async (pin: string) => {
    setPinModalError(null)
    const ok = await verifyPin(pin)
    if (!ok) {
      setPinModalError('That PIN is not correct.')
      return
    }
    const expiresAt = Date.now() + SESSION_MS
    setPinSession({ pin, expiresAt })
    setPinModalOpen(false)
    pendingPinRef.current?.(pin)
    pendingPinRef.current = null
  }, [])

  const handlePinModalClose = useCallback(() => {
    setPinModalOpen(false)
    setPinModalError(null)
    pendingPinRef.current?.(null)
    pendingPinRef.current = null
  }, [])

  const value = useMemo<AppStateContextValue>(
    () => ({
      bootstrap,
      bootstrapError,
      reloadBootstrap,
      deviceId,
      setDeviceId,
      selectedDevice,
      pinConfigured,
      getPinForChanges,
      clearPinSession,
    }),
    [
      bootstrap,
      bootstrapError,
      reloadBootstrap,
      deviceId,
      setDeviceId,
      selectedDevice,
      pinConfigured,
      getPinForChanges,
      clearPinSession,
    ],
  )

  return (
    <AppStateContext.Provider value={value}>
      {children}
      <PinUnlockModal
        key={pinModalKey}
        open={pinModalOpen}
        errorText={pinModalError}
        onClose={handlePinModalClose}
        onSubmit={handlePinModalSubmit}
      />
    </AppStateContext.Provider>
  )
}
