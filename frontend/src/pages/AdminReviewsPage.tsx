import { useEffect, useState } from 'react'
import { deleteReview, getAllReviews, replyToReview } from '../api/client'
import type { ReviewDto } from '../api/types'
import { useTranslation } from '../i18n/useTranslation'
import { usePageTitle } from '../hooks/usePageTitle'

function AdminReviewRow({
  review,
  onReplied,
  onDeleted,
}: {
  review: ReviewDto
  onReplied: (updated: ReviewDto) => void
  onDeleted: (id: number) => void
}) {
  const { t } = useTranslation()
  const [reply, setReply] = useState(review.adminReply ?? '')
  const [sending, setSending] = useState(false)

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!reply.trim()) return
    setSending(true)
    try {
      const updated = await replyToReview(review.id, reply.trim())
      onReplied(updated)
    } finally {
      setSending(false)
    }
  }

  async function handleDelete() {
    await deleteReview(review.id)
    onDeleted(review.id)
  }

  return (
    <li className="review-item">
      <div className="review-item-header">
        <span className="review-author">
          {review.storeName} · {review.rating}/5 · {review.displayName ?? t('reviews.anonymousLabel')}
        </span>
        <button type="button" className="site-review-delete" onClick={handleDelete}>
          {t('siteReviews.delete')}
        </button>
      </div>
      <p className="review-comment">{review.comment}</p>
      <form className="ticket-form" onSubmit={handleSubmit}>
        <textarea
          className="review-comment-input"
          placeholder={t('admin.replyPlaceholder')}
          value={reply}
          onChange={(e) => setReply(e.target.value)}
          rows={2}
          maxLength={2000}
        />
        <button type="submit" className="btn-primary" disabled={sending || !reply.trim()}>
          {t('admin.replySubmit')}
        </button>
      </form>
    </li>
  )
}

export function AdminReviewsPage() {
  const { t } = useTranslation()
  const [reviews, setReviews] = useState<ReviewDto[]>([])

  usePageTitle(t('admin.allReviews'))

  useEffect(() => {
    let cancelled = false
    getAllReviews().then((list) => {
      if (!cancelled) setReviews(list)
    })
    return () => {
      cancelled = true
    }
  }, [])

  function handleReplied(updated: ReviewDto) {
    setReviews((prev) => prev.map((r) => (r.id === updated.id ? updated : r)))
  }

  function handleDeleted(id: number) {
    setReviews((prev) => prev.filter((r) => r.id !== id))
  }

  return (
    <section className="account">
      <h1 className="page-title">{t('admin.allReviews')}</h1>

      <ul className="review-list">
        {reviews.map((review) => (
          <AdminReviewRow key={review.id} review={review} onReplied={handleReplied} onDeleted={handleDeleted} />
        ))}
      </ul>
    </section>
  )
}
