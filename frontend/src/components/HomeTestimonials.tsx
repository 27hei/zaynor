import { useEffect, useState } from 'react'
import type { ReviewDto } from '../api/types'
import { getFeaturedReviews } from '../api/client'
import { useTranslation } from '../i18n/useTranslation'

const STAR_PATH = 'M12 2.5l2.9 6.3 6.9.7-5.2 4.6 1.5 6.8L12 17.6l-6.1 3.3 1.5-6.8L2.2 9.5l6.9-.7L12 2.5z'

/**
 * A curated highlight of the highest-rated recent reviews across every
 * store, for social proof on the homepage. Not a filter on what's visible
 * elsewhere — a store's own page still shows every review it has, good and
 * bad. Renders nothing at all once there simply aren't any reviews yet, so
 * the homepage never shows a fake/empty placeholder.
 */
export function HomeTestimonials() {
  const { t } = useTranslation()
  const [reviews, setReviews] = useState<ReviewDto[]>([])

  useEffect(() => {
    let cancelled = false
    getFeaturedReviews().then((list) => {
      if (!cancelled) setReviews(list)
    })
    return () => {
      cancelled = true
    }
  }, [])

  if (reviews.length === 0) {
    return null
  }

  return (
    <section className="home-testimonials" aria-label={t('home.testimonialsTitle')}>
      <h2 className="home-testimonials-title">{t('home.testimonialsTitle')}</h2>
      <div className="home-testimonials-grid">
        {reviews.map((review) => (
          <div key={review.id} className="home-testimonial-card">
            <div className="home-testimonial-store">{review.storeName}</div>
            <div className="home-testimonial-stars">
              {[1, 2, 3, 4, 5].map((n) => (
                <svg
                  key={n}
                  width="18"
                  height="18"
                  viewBox="0 0 24 24"
                  fill={n <= review.rating ? 'currentColor' : 'none'}
                  stroke="currentColor"
                  strokeWidth="1.75"
                  aria-hidden="true"
                >
                  <path d={STAR_PATH} />
                </svg>
              ))}
            </div>
            <p className="home-testimonial-comment">{review.comment}</p>
          </div>
        ))}
      </div>
    </section>
  )
}
