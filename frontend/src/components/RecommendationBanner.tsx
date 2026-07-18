import type { Recommendation } from '../api/types'
import { formatPrice } from '../format'

interface RecommendationBannerProps {
  recommendation: Recommendation
}

export function RecommendationBanner({ recommendation }: RecommendationBannerProps) {
  const { bestStoreName, bestPrice, currency, savings } = recommendation

  return (
    <div className="recommendation" role="status">
      <div className="recommendation-badge">Best deal</div>
      <div className="recommendation-body">
        <p className="recommendation-headline">
          {bestStoreName} — {formatPrice(bestPrice, currency)}
        </p>
        {savings > 0 && (
          <p className="recommendation-savings">
            Save up to {formatPrice(savings, currency)}
          </p>
        )}
        <p className="recommendation-message">{recommendation.message}</p>
      </div>
    </div>
  )
}
