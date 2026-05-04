import { createContext, useContext } from 'react'
import type { AdminBootstrapDevice, AdminBootstrapResponse } from '@/lib/types'

export type AppStateContextValue = {
  bootstrap: AdminBootstrapResponse | null
  bootstrapError: string | null
  reloadBootstrap: () => Promise<void>
  deviceId: string
  setDeviceId: (id: string) => void
  selectedDevice: AdminBootstrapDevice | undefined
  pinConfigured: boolean
  getPinForChanges: () => Promise<string | null>
  clearPinSession: () => void
}

export const AppStateContext = createContext<AppStateContextValue | null>(null)

export function useAppState(): AppStateContextValue {
  const ctx = useContext(AppStateContext)
  if (!ctx) throw new Error('useAppState must be used within AppStateProvider')
  return ctx
}
