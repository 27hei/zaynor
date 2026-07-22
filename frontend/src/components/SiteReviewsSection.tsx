import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import type { SiteReviewDto } from '../api/types'
import { deleteSiteReview, getSiteReviews, submitSiteReview } from '../api/client'
import { useAuth } from '../auth/useAuth'
import { useTranslation } from '../i18n/useTranslation'

const STAR_PATH = 'M12 2.5l2.9 6.3 6.9.7-5.2 4.6 1.5 6.8L12 17.6l-6.1 3.3 1.5-6.8L2.2 9.5l6.9-.7L12 2.5z'

function Star({ filled }: { filled: boolean }) {
  return (
    <svg width="22" height="22" viewBox="0 0 24 24" fill={filled ? 'currentColor' : 'none'} stroke="currentColor" strokeWidth="1.75" aria-hidden="true">
      <path d={STAR_PATH} />
    </svg>
  )
}

const VISIBLE_BY_DEFAULT = 6

/**
 * Reviews of Zaynor itself (not a specific store) — shown on the homepage
 * for every visitor. Any logged-in user can rate/comment on their experience
 * with the site. Unlike store reviews, the admin can delete a review here
 * outright (moderating content about the founder's own platform, not
 * suppressing legitimate feedback about a third-party store).
 */
export function SiteReviewsSection() {
  const { t } = useTranslation()
  const { user } = useAuth()
  const [reviews, setReviews] = useState<SiteReviewDto[]>([])
  const [loading, setLoading] = useState(true)
  const [expanded, setExpanded] = useState(false)
  const [rating, setRating] = useState(0)
  const [hoverRating, setHoverRating] = useState(0)
  const [comment, setComment] = useState('')
  const [displayName, setDisplayName] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState(false)

  useEffect(() => {
    let cancelled = false
    getSiteReviews().then((list) => {
      if (!cancelled) {
        setReviews(list)
        setLoading(false)
      }
    })
    return () => {
      cancelled = true
    }
  }, [])

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (rating === 0 || !comment.trim()) return

    setSubmitting(true)
    setError(false)
    try {
      const created = await submitSiteReview(rating, comment.trim(), displayName.trim() || null)
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

  async function handleDelete(id: number) {
    try {
      await deleteSiteReview(id)
      setReviews((prev) => prev.filter((r) => r.id !== id))
    } catch {
      /* leave the review in place if deletion failed */
    }
  }

  const visibleReviews = expanded ? reviews : reviews.slice(0, VISIBLE_BY_DEFAULT)

  return (
    <section className="home-testimonials site-reviews-section" aria-label={t('siteReviews.title')}>
      <h2 className="home-testimonials-title">{t('siteReviews.title')}</h2>

      {user ? (
        <form className="review-form" onSubmit={handleSubmit}>
          <div className="review-stars-input" role="radiogroup" aria-label={t('reviews.ratingLabel')} onMouseLeave={() => setHoverRating(0)}>
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
            placeholder={t('siteReviews.commentPlaceholder')}
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
          <Link to="/login">{t('siteReviews.loginToReview')}</Link>
        </p>
      )}

      {!loading && reviews.length === 0 && <p className="hint">{t('siteReviews.empty')}</p>}

      <div className="home-testimonials-grid">
        {visibleReviews.map((review) => (
          <div key={review.id} className="home-testimonial-card">
            <div className="home-testimonial-stars">
              {[1, 2, 3, 4, 5].map((n) => (
                <Star key={n} filled={n <= review.rating} />
              ))}
            </div>
            <p className="home-testimonial-comment">{review.comment}</p>
            <div className="site-review-footer">
              <span className="review-author">{review.displayName ?? t('reviews.anonymousLabel')}</span>
              {user?.isAdmin && (
                <button type="button" className="site-review-delete" onClick={() => handleDelete(review.id)}>
                  {t('siteReviews.delete')}
                </button>
              )}
            </div>
          </div>
        ))}
      </div>

      {reviews.length > VISIBLE_BY_DEFAULT && !expanded && (
        <button type="button" className="offer-list-expand" onClick={() => setExpanded(true)}>
          {t('siteReviews.showAll')}
        </button>
      )}
    </section>
  )
}
