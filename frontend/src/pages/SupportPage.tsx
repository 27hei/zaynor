import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { createTicket, getMyTickets } from '../api/client'
import type { SupportTicketDto } from '../api/types'
import { useTranslation } from '../i18n/useTranslation'
import { usePageTitle } from '../hooks/usePageTitle'

export function SupportPage() {
  const { t, lang } = useTranslation()
  const [tickets, setTickets] = useState<SupportTicketDto[]>([])
  const [loading, setLoading] = useState(true)
  const [subject, setSubject] = useState('')
  const [message, setMessage] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState(false)

  usePageTitle(t('support.title'))

  useEffect(() => {
    let cancelled = false
    getMyTickets()
      .then((list) => {
        if (!cancelled) {
          setTickets(list)
          setLoading(false)
        }
      })
      .catch(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [])

  const dateFormat = new Intl.DateTimeFormat(lang === 'ar' ? 'ar' : 'en', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
  })

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!subject.trim() || !message.trim()) return

    setSubmitting(true)
    setError(false)
    try {
      const created = await createTicket(subject.trim(), message.trim())
      setTickets((prev) => [created, ...prev])
      setSubject('')
      setMessage('')
    } catch {
      setError(true)
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <section className="account">
      <h1 className="page-title">{t('support.title')}</h1>

      <form className="ticket-form" onSubmit={handleSubmit}>
        <input
          className="review-name-input"
          placeholder={t('support.subjectLabel')}
          value={subject}
          onChange={(e) => setSubject(e.target.value)}
          maxLength={200}
        />
        <textarea
          className="review-comment-input"
          placeholder={t('support.messageLabel')}
          value={message}
          onChange={(e) => setMessage(e.target.value)}
          rows={4}
          maxLength={4000}
        />
        <button type="submit" className="btn-primary" disabled={submitting || !subject.trim() || !message.trim()}>
          {t('support.submit')}
        </button>
        {error && <p className="hint hint-error">{t('reviews.submitError')}</p>}
      </form>

      {!loading && tickets.length === 0 && <p className="hint">{t('support.empty')}</p>}

      <ul className="item-list">
        {tickets.map((ticket) => (
          <li className="item-row" key={ticket.id}>
            <Link to={`/support/${ticket.id}`} className="item-info">
              <span className="item-name-row">
                <span className="item-name">{ticket.subject}</span>
                <span className={ticket.isClosed ? 'ticket-status-badge ticket-status-closed' : 'ticket-status-badge ticket-status-open'}>
                  {ticket.isClosed ? t('support.statusClosed') : t('support.statusOpen')}
                </span>
              </span>
              <span className="item-date">{dateFormat.format(new Date(ticket.updatedAt))}</span>
            </Link>
          </li>
        ))}
      </ul>
    </section>
  )
}
