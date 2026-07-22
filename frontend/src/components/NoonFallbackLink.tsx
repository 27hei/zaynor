import { outboundUrl } from '../api/client'
import { useTranslation } from '../i18n/useTranslation'
import { StoreLogo } from './StoreLogo'
import { DiscoveryIcon } from './icons'

interface NoonFallbackLinkProps {
  query: string
}

/**
 * Noon isn't a real-time data source (spec: no official search API) — when
 * it isn't already among the offers, this links straight to Noon's own
 * search results for the query instead. Styled as an offer card (same
 * classes as OfferList) so it reads as "one more store to check", not a
 * separate/lesser element. The link rides through /api/out, which tags any
 * noon.com URL with the affiliate suffix automatically, so every search
 * stays monetizable even when it's outside the curated catalog and live
 * feeds.
 */
export function NoonFallbackLink({ query }: NoonFallbackLinkProps) {
  const { t } = useTranslation()
  const noonSearchUrl = `https://www.noon.com/saudi-en/search/?q=${encodeURIComponent(query)}`

  return (
    <a
      className="offer-card offer-noon-fallback"
      href={outboundUrl(noonSearchUrl, 'Noon', query)}
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
