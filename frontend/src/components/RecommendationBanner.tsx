import type { Recommendation } from '../api/types'
import { formatPrice } from '../format'
import { useTranslation } from '../i18n/useTranslation'

interface RecommendationBannerProps {
  recommendation: Recommendation
  /** Outbound (affiliate) URL of the best offer, so the banner itself can act. */
  bestUrl?: string
}

export function RecommendationBanner({ recommendation, bestUrl }: RecommendationBannerProps) {
  const { t } = useTranslation()
  const { bestStoreName, bestPrice, currency, comparedStoreName, comparedPrice, savings } =
    recommendation

  // Build the message from structured fields so it localizes correctly, rather
  // than relying on the English message string the backend composes.
  const message =
    savings > 0
      ? t('reco.message', {
          store: bestStoreName,
          price: formatPrice(bestPrice, currency),
          savings: formatPrice(savings, currency),
          comparedStore: comparedStoreName,
          comparedPrice: formatPrice(comparedPrice, currency),
        })
      : ''

  return (
    <div className="recommendation" role="status">
      <div className="recommendation-badge">{t('reco.badge')}</div>
      <div className="recommendation-body">
        <p className="recommendation-headline">
          {bestStoreName} — {formatPrice(bestPrice, currency)}
        </p>
        {savings > 0 && (
          <p className="recommendation-savings">
            {t('reco.saveUpTo', { amount: formatPrice(savings, currency) })}
          </p>
        )}
        {message && <p className="recommendation-message">{message}</p>}
        {bestUrl && (
          <a
            className="reco-cta"
            href={bestUrl}
            target="_blank"
            rel="noopener noreferrer sponsored"
          >
            {t('reco.cta')}
            <span aria-hidden="true" className="reco-cta-arrow">
              →
            </span>
          </a>
        )}
      </div>
    </div>
  )
}
