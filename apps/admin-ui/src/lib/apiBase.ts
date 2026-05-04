/** In dev, Vite proxies `/api` to the local guardian service. In production builds, set `VITE_API_ORIGIN` if the UI is not served from the same host as the API. */
export function apiUrl(path: string): string {
  const trimmed = path.startsWith('/') ? path : `/${path}`
  if (import.meta.env.DEV) return trimmed
  const origin = (import.meta.env.VITE_API_ORIGIN as string | undefined)?.replace(/\/$/, '')
  return `${origin ?? 'http://127.0.0.1:5050'}${trimmed}`
}
