import { useTranslation } from '../i18n/useTranslation'

interface StoreRatingProps {
  rating: number
  ratingCount: number | null
}

function formatCount(count: number): string {
  if (count >= 1000) return `${(count / 1000).toFixed(count >= 10000 ? 0 : 1)}k`
  return String(count)
}

/**
 * A star rating as reported by the data source for a specific store's
 * listing — never a fabricated/averaged "product" rating, since ratings are
 * genuinely per-merchant (spec: honesty standard — no invented data).
 */
export function StoreRating({ rating, ratingCount }: StoreRatingProps) {
  const { t } = useTranslation()

  return (
    <span
      className="store-rating"
      aria-label={t('offer.ratingLabel', { rating: rating.toFixed(1) })}
    >
      <svg className="store-rating-star" width="13" height="13" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
        <path d="M12 2.5l2.9 6.3 6.9.7-5.2 4.6 1.5 6.8L12 17.6l-6.1 3.3 1.5-6.8L2.2 9.5l6.9-.7L12 2.5z" />
      </svg>
      <span className="store-rating-value">{rating.toFixed(1)}</span>
      {ratingCount != null && (
        <span className="store-rating-count">({formatCount(ratingCount)})</span>
      )}
    </span>
  )
}
