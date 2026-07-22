import type { AggregatedOffer } from './api/types'

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

type Translate = (key: string, vars?: Record<string, string | number>) => string

/** Shared between OfferList and ProductDetailPage so both show the same shipping/delivery line for an offer. */
export function shippingLabel(offer: AggregatedOffer, t: Translate): string | null {
  const parts: string[] = []
  if (offer.freeShipping) {
    parts.push(t('offer.freeShipping'))
  }
  if (offer.deliveryDays != null) {
    parts.push(
      offer.deliveryDays <= 1 ? t('offer.deliveryNextDay') : t('offer.delivery', { days: offer.deliveryDays }),
    )
  }
  return parts.length > 0 ? parts.join(' · ') : null
}
