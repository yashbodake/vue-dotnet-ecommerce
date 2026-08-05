/** Resolve product image URLs for the SPA (legacy paths are not hosted). */
export function productImageUrl(url: string | null | undefined): string | null {
  if (!url) return null
  const trimmed = url.trim()
  if (!trimmed) return null
  if (/^https?:\/\//i.test(trimmed)) return trimmed
  // Legacy IIS paths like /Content/images/... are not served by Vue
  return null
}

export function productImageFallback(name: string): string {
  const initial = (name?.trim()?.[0] || '?').toUpperCase()
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="400" height="400" viewBox="0 0 400 400">
  <rect width="400" height="400" fill="#efe8db"/>
  <rect x="20" y="20" width="360" height="360" fill="#f6f1e7" stroke="#d6cdbb" stroke-width="1.5"/>
  <text x="200" y="232" text-anchor="middle" font-family="'Fraunces', Georgia, 'Times New Roman', serif" font-weight="500" font-size="150" fill="#8c8475" letter-spacing="-0.04em">${initial}</text>
</svg>`
  return `data:image/svg+xml;charset=utf-8,${encodeURIComponent(svg)}`
}
