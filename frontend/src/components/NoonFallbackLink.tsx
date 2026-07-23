import { outboundUrl } from '../api/client'
import { useTranslation } from '../i18n/useTranslation'
import { StoreLogo } from './StoreLogo'
import { DiscoveryIcon } from './icons'

interface NoonFallbackLinkProps {
  query: string
}

/**
 * Noon's affiliate network (Impact-style) attributes commission through its
 * own tracking short-link (s.noon.com/...), not by appending params to a
 * plain noon.com URL — so when Noon isn't already among the offers, this
 * sends visitors through the real campaign link rather than an untagged
 * noon.com search URL that would never earn anything. It's a general
 * storefront link (not query-specific), since generating a per-product
 * tracking link requires Noon's partner dashboard. Styled as an offer card
 * (same classes as OfferList) so it reads as "one more store to check".
 * s.noon.com is covered by /api/out's existing noon.com host allowlist, so
 * it passes through untouched (pure click logging, no re-tagging needed —
 * the tracking is already baked into the link itself).
 */
const NOON_TRACKING_URL = 'https://s.noon.com/dBcC-E2fLJ8'

export function NoonFallbackLink({ query }: NoonFallbackLinkProps) {
  const { t } = useTranslation()

  return (
    <a
      className="offer-card offer-noon-fallback"
      href={outboundUrl(NOON_TRACKING_URL, 'Noon', query)}
      target="_blank"
      rel="noopener noreferrer sponsored"
    >
      <span className="offer-card-image offer-card-image-search" aria-hidden="true">
        <DiscoveryIcon />
      </span>
      <span className="offer-card-store">
        <StoreLogo storeName="Noon" />
        <span className="offer-card-store-name">Noon</span>
      </span>
      <span className="offer-card-price offer-card-price-search">{t('results.noonFallbackText')}</span>
    </a>
  )
}
