import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import type { ReviewDto } from '../api/types'
import { getStoreReviews, submitReview } from '../api/client'
import { useAuth } from '../auth/useAuth'
import { useTranslation } from '../i18n/useTranslation'

const STAR_PATH = 'M12 2.5l2.9 6.3 6.9.7-5.2 4.6 1.5 6.8L12 17.6l-6.1 3.3 1.5-6.8L2.2 9.5l6.9-.7L12 2.5z'

function Star({ filled }: { filled: boolean }) {
  return (
    <svg
      width="22"
      height="22"
      viewBox="0 0 24 24"
      fill={filled ? 'currentColor' : 'none'}
      stroke="currentColor"
      strokeWidth="1.75"
      aria-hidden="true"
    >
      <path d={STAR_PATH} />
    </svg>
  )
}

interface ReviewsSectionProps {
  storeName: string
}

/**
 * Reviews for one store — always shows every review regardless of rating
 * (founder's call: hiding negative reviews would be deceptive). The admin's
 * reply, when present, renders as a distinct block right under the review.
 */
export function ReviewsSection({ storeName }: ReviewsSectionProps) {
  const { t } = useTranslation()
  const { user } = useAuth()
  const [reviews, setReviews] = useState<ReviewDto[]>([])
  const [loading, setLoading] = useState(true)
  const [rating, setRating] = useState(0)
  const [hoverRating, setHoverRating] = useState(0)
  const [comment, setComment] = useState('')
  const [displayName, setDisplayName] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState(false)

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    getStoreReviews(storeName).then((list) => {
      if (!cancelled) {
        setReviews(list)
        setLoading(false)
      }
    })
    return () => {
      cancelled = true
    }
  }, [storeName])

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (rating === 0 || !comment.trim()) return

    setSubmitting(true)
    setError(false)
    try {
      const created = await submitReview(storeName, rating, comment.trim(), displayName.trim() || null)
      setReviews((prev) => [created, ...prev])
      setRating(0)
      setComment('')
      setDisplayName('')
    } catch {
      setError(true)
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <section className="reviews-section">
      <h2 className="product-detail-section-title">{t('reviews.title')}</h2>

      {user ? (
        <form className="review-form" onSubmit={handleSubmit}>
          <div
            className="review-stars-input"
            role="radiogroup"
            aria-label={t('reviews.ratingLabel')}
            onMouseLeave={() => setHoverRating(0)}
          >
            {[1, 2, 3, 4, 5].map((n) => (
              <button
                key={n}
                type="button"
                role="radio"
                aria-checked={rating === n}
                aria-label={String(n)}
                onMouseEnter={() => setHoverRating(n)}
                onClick={() => setRating(n)}
              >
                <Star filled={n <= (hoverRating || rating)} />
              </button>
            ))}
          </div>
          <textarea
            className="review-comment-input"
            placeholder={t('reviews.commentPlaceholder')}
            value={comment}
            onChange={(e) => setComment(e.target.value)}
            maxLength={2000}
            rows={3}
          />
          <input
            className="review-name-input"
            placeholder={t('reviews.displayNamePlaceholder')}
            value={displayName}
            onChange={(e) => setDisplayName(e.target.value)}
            maxLength={80}
          />
          <button type="submit" className="btn-primary" disabled={submitting || rating === 0 || !comment.trim()}>
            {t('reviews.submit')}
          </button>
          {error && <p className="hint hint-error">{t('reviews.submitError')}</p>}
        </form>
      ) : (
        <p className="hint">
          <Link to="/login">{t('reviews.loginToReview')}</Link>
        </p>
      )}

      {!loading && reviews.length === 0 && <p className="hint">{t('reviews.empty')}</p>}

      <ul className="review-list">
        {reviews.map((review) => (
          <li key={review.id} className="review-item">
            <div className="review-item-header">
              {[1, 2, 3, 4, 5].map((n) => (
                <Star key={n} filled={n <= review.rating} />
              ))}
              <span className="review-author">{review.displayName ?? t('reviews.anonymousLabel')}</span>
            </div>
            <p className="review-comment">{review.comment}</p>
            {review.adminReply && (
              <div className="review-admin-reply">
                <span className="review-admin-reply-label">{t('reviews.adminReplyLabel')}</span>
                <p>{review.adminReply}</p>
              </div>
            )}
          </li>
        ))}
      </ul>
    </section>
  )
}
