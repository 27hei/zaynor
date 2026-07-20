// The stores Zaynor actively compares (spec Section 6, 20.3) — the single
// source of truth for the hero trust row and the footer trust row.
export const TRACKED_STORE_NAMES = ['Amazon.sa', 'Noon', 'Jarir', 'Extra', 'AliExpress']

// Recognizable store identity colors (competitive analysis Section 6.C) —
// shared by the offer rows and the price-history chart legend. Logo doubles
// as a fallback source; the row falls back to the initial-letter avatar
// (colored per this same table) if the logo file 404s.
export const STORE_BRAND: Record<string, { bg: string; fg: string; logo: string }> = {
  'Amazon.sa': { bg: '#232f3e', fg: '#ff9900', logo: '/store-logos/amazon.png' },
  Noon: { bg: '#feee00', fg: '#3a3a3a', logo: '/store-logos/noon.png' },
  Jarir: { bg: '#d71920', fg: '#ffffff', logo: '/store-logos/jarir.png' },
  Extra: { bg: '#0057b8', fg: '#ffffff', logo: '/store-logos/extra.png' },
  AliExpress: { bg: '#e43225', fg: '#ffffff', logo: '/store-logos/aliexpress.png' },
}

/** A stroke color for charts: the store's strongest identity color. */
export function storeLineColor(storeName: string): string {
  const brand = STORE_BRAND[storeName]
  if (!brand) {
    return '#6a7a73'
  }
  // Yellow-on-white lines are unreadable; Noon's chart line uses its dark fg.
  return brand.bg === '#feee00' ? '#b8a800' : brand.bg
}
