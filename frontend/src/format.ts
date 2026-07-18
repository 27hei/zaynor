/** Formats a price with its currency, e.g. 4050 + "SAR" → "SAR 4,050.00". */
export function formatPrice(amount: number, currency: string): string {
  try {
    return new Intl.NumberFormat('en', {
      style: 'currency',
      currency,
      currencyDisplay: 'code',
    }).format(amount)
  } catch {
    // Fall back if the currency code isn't recognized by the runtime.
    return `${currency} ${amount.toFixed(2)}`
  }
}
