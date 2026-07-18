// Recognizable store identity colors (competitive analysis Section 6.C) —
// shared by the offer rows and the price-history chart legend.
export const STORE_BRAND: Record<string, { bg: string; fg: string }> = {
  'Amazon.sa': { bg: '#232f3e', fg: '#ff9900' },
  Noon: { bg: '#feee00', fg: '#3a3a3a' },
  Jarir: { bg: '#d71920', fg: '#ffffff' },
  Extra: { bg: '#0057b8', fg: '#ffffff' },
  AliExpress: { bg: '#e43225', fg: '#ffffff' },
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
