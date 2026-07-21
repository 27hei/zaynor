import { outboundUrl } from '../api/client'
import { useTranslation } from '../i18n/useTranslation'
import { StoreLogo } from './StoreLogo'

interface NoonFallbackLinkProps {
  query: string
}

/**
 * Noon isn't a real-time data source (spec: no official search API) — when
 * it isn't already among the offers, this links straight to Noon's own
 * search results for the query instead. The link rides through /api/out,
 * which tags any noon.com URL with the affiliate suffix automatically, so
 * every search stays monetizable even when it's outside the curated
 * catalog and live feeds.
 */
export function NoonFallbackLink({ query }: NoonFallbackLinkProps) {
  const { t } = useTranslation()
  const noonSearchUrl = `https://www.noon.com/saudi-en/search/?q=${encodeURIComponent(query)}`

  return (
    <a
      className="noon-fallback"
      href={outboundUrl(noonSearchUrl, 'Noon', query)}
      target="_blank"
      rel="noopener noreferrer sponsored"
    >
      <StoreLogo storeName="Noon" />
      <span className="noon-fallback-text">{t('results.noonFallbackText')}</span>
      <span className="noon-fallback-cta">{t('results.noonFallbackCta')}</span>
    </a>
  )
}
