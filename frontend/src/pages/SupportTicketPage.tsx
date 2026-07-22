import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { addTicketMessage, getMyTicket } from '../api/client'
import type { SupportTicketDetailDto } from '../api/types'
import { useTranslation } from '../i18n/useTranslation'
import { usePageTitle } from '../hooks/usePageTitle'

export function SupportTicketPage() {
  const { t, lang } = useTranslation()
  const { id } = useParams<{ id: string }>()
  const ticketId = Number(id)
  const [ticket, setTicket] = useState<SupportTicketDetailDto | null>(null)
  const [reply, setReply] = useState('')
  const [sending, setSending] = useState(false)
  const [error, setError] = useState(false)

  usePageTitle(ticket?.subject ?? t('support.title'))

  useEffect(() => {
    let cancelled = false
    getMyTicket(ticketId)
      .then((detail) => {
        if (!cancelled) setTicket(detail)
      })
      .catch(() => {
        if (!cancelled) setTicket(null)
      })
    return () => {
      cancelled = true
    }
  }, [ticketId])

  const timeFormat = new Intl.DateTimeFormat(lang === 'ar' ? 'ar' : 'en', {
    dateStyle: 'medium',
    timeStyle: 'short',
  })

  async function handleReply(e: React.FormEvent) {
    e.preventDefault()
    if (!reply.trim() || !ticket) return

    setSending(true)
    setError(false)
    try {
      const message = await addTicketMessage(ticketId, reply.trim())
      setTicket({ ...ticket, isClosed: false, messages: [...ticket.messages, message] })
      setReply('')
    } catch {
      setError(true)
    } finally {
      setSending(false)
    }
  }

  if (!ticket) {
    return (
      <section className="product-detail-fallback">
        <p className="hint">{t('support.notFound')}</p>
        <Link to="/support" className="btn-primary">
          {t('support.backToTickets')}
        </Link>
      </section>
    )
  }

  return (
    <section className="account">
      <Link to="/support" className="product-detail-back">
        {t('productDetail.backToResults')}
      </Link>
      <h1 className="page-title">{ticket.subject}</h1>

      <div className="ticket-thread">
        {ticket.messages.map((message) => (
          <div
            key={message.id}
            className={message.isFromAdmin ? 'support-message support-message-admin' : 'support-message'}
          >
            <p>{message.body}</p>
            <span className="item-date">{timeFormat.format(new Date(message.createdAt))}</span>
          </div>
        ))}
      </div>

      {ticket.isClosed && <p className="hint">{t('support.closedNotice')}</p>}

      <form className="ticket-form" onSubmit={handleReply}>
        <textarea
          className="review-comment-input"
          placeholder={t('support.messageLabel')}
          value={reply}
          onChange={(e) => setReply(e.target.value)}
          rows={3}
          maxLength={4000}
        />
        <button type="submit" className="btn-primary" disabled={sending || !reply.trim()}>
          {t('support.reply')}
        </button>
        {error && <p className="hint hint-error">{t('reviews.submitError')}</p>}
      </form>
    </section>
  )
}
