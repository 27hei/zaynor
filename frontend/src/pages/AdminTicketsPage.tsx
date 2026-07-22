import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { getAllTickets } from '../api/client'
import type { AdminSupportTicketDto } from '../api/types'
import { useTranslation } from '../i18n/useTranslation'
import { usePageTitle } from '../hooks/usePageTitle'

export function AdminTicketsPage() {
  const { t, lang } = useTranslation()
  const [tickets, setTickets] = useState<AdminSupportTicketDto[]>([])

  usePageTitle(t('admin.ticketInbox'))

  useEffect(() => {
    let cancelled = false
    getAllTickets().then((list) => {
      if (!cancelled) setTickets(list)
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

  return (
    <section className="account">
      <h1 className="page-title">{t('admin.ticketInbox')}</h1>

      <ul className="item-list">
        {tickets.map((ticket) => (
          <li className="item-row" key={ticket.id}>
            <Link to={`/admin/tickets/${ticket.id}`} className="item-info">
              <span className="item-name-row">
                <span className="item-name">{ticket.subject}</span>
                <span className={ticket.isClosed ? 'ticket-status-badge ticket-status-closed' : 'ticket-status-badge ticket-status-open'}>
                  {ticket.isClosed ? t('support.statusClosed') : t('support.statusOpen')}
                </span>
              </span>
              <span className="item-date">
                {ticket.userEmail} · {dateFormat.format(new Date(ticket.updatedAt))}
              </span>
            </Link>
          </li>
        ))}
      </ul>
    </section>
  )
}
