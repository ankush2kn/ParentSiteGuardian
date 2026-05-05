/**
 * Dev: Vite proxies `/api` to the guardian service.
 * Prod: same-origin `/api` when the UI is served from the API (e.g. `/admin`) or set `VITE_API_ORIGIN` at build time.
 * `npm run preview` proxies `/api` like dev when using relative URLs.
 */
export function apiUrl(path: string): string {
  const trimmed = path.startsWith('/') ? path : `/${path}`
  if (import.meta.env.DEV) return trimmed
  const origin = (import.meta.env.VITE_API_ORIGIN as string | undefined)?.replace(/\/$/, '')
  if (origin) return `${origin}${trimmed}`
  return trimmed
}
