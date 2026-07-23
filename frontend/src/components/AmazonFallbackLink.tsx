import { outboundUrl } from '../api/client'
import { useTranslation } from '../i18n/useTranslation'
import { StoreLogo } from './StoreLogo'
import { DiscoveryIcon } from './icons'

interface AmazonFallbackLinkProps {
  query: string
}

/**
 * Amazon.sa only shows up when a live source happens to surface it (no
 * dedicated, always-on Amazon feed is wired up) — when it isn't already
 * among the offers, this links straight to Amazon's own search results for
 * the query instead, mirroring NoonFallbackLink. The link rides through
 * /api/out, which tags any amazon.sa URL with the configured Associates tag
 * automatically, so every search stays monetizable even when Amazon didn't
 * turn up as a priced offer this time.
 */
export function AmazonFallbackLink({ query }: AmazonFallbackLinkProps) {
  const { t } = useTranslation()
  const amazonSearchUrl = `https://www.amazon.sa/s?k=${encodeURIComponent(query)}`

  return (
    <a
      className="offer-card offer-noon-fallback"
      href={outboundUrl(amazonSearchUrl, 'Amazon.sa', query)}
      target="_blank"
      rel="noopener noreferrer sponsored"
    >
      <span className="offer-card-image offer-card-image-search" aria-hidden="true">
        <DiscoveryIcon />
      </span>
      <span className="offer-card-store">
        <StoreLogo storeName="Amazon.sa" />
        <span className="offer-card-store-name">Amazon.sa</span>
      </span>
      <span className="offer-card-price offer-card-price-search">{t('results.amazonFallbackText')}</span>
    </a>
  )
}
