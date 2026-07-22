import { useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { addAdminReply, closeTicket, getAdminTicket } from '../api/client'
import type { AdminSupportTicketDetailDto } from '../api/types'
import { useTranslation } from '../i18n/useTranslation'
import { usePageTitle } from '../hooks/usePageTitle'

export function AdminTicketThreadPage() {
  const { t, lang } = useTranslation()
  const navigate = useNavigate()
  const { id } = useParams<{ id: string }>()
  const ticketId = Number(id)
  const [ticket, setTicket] = useState<AdminSupportTicketDetailDto | null>(null)
  const [reply, setReply] = useState('')
  const [sending, setSending] = useState(false)
  const [closing, setClosing] = useState(false)

  usePageTitle(ticket?.subject ?? t('admin.ticketInbox'))

  useEffect(() => {
    let cancelled = false
    getAdminTicket(ticketId)
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
    try {
      const message = await addAdminReply(ticketId, reply.trim())
      setTicket({ ...ticket, messages: [...ticket.messages, message] })
      setReply('')
    } finally {
      setSending(false)
    }
  }

  async function handleClose() {
    setClosing(true)
    try {
      await closeTicket(ticketId)
      navigate('/admin/tickets')
    } finally {
      setClosing(false)
    }
  }

  if (!ticket) {
    return (
      <section className="product-detail-fallback">
        <p className="hint">{t('support.notFound')}</p>
        <Link to="/admin/tickets" className="btn-primary">
          {t('admin.ticketInbox')}
        </Link>
      </section>
    )
  }

  return (
    <section className="account">
      <Link to="/admin/tickets" className="product-detail-back">
        {t('productDetail.backToResults')}
      </Link>
      <h1 className="page-title">{ticket.subject}</h1>
      <p className="page-subtitle">{ticket.userEmail}</p>

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

      {!ticket.isClosed && (
        <button type="button" className="btn btn-ghost" onClick={handleClose} disabled={closing}>
          {t('admin.closeTicket')}
        </button>
      )}

      <form className="ticket-form" onSubmit={handleReply}>
        <textarea
          className="review-comment-input"
          placeholder={t('admin.replyPlaceholder')}
          value={reply}
          onChange={(e) => setReply(e.target.value)}
          rows={3}
          maxLength={4000}
        />
        <button type="submit" className="btn-primary" disabled={sending || !reply.trim()}>
          {t('admin.replySubmit')}
        </button>
      </form>
    </section>
  )
}
