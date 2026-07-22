import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { getAllReviews, getAllTickets } from '../api/client'
import { useTranslation } from '../i18n/useTranslation'
import { usePageTitle } from '../hooks/usePageTitle'

export function AdminDashboardPage() {
  const { t } = useTranslation()
  const [openTickets, setOpenTickets] = useState(0)
  const [totalReviews, setTotalReviews] = useState(0)

  usePageTitle(t('admin.dashboardTitle'))

  useEffect(() => {
    let cancelled = false
    Promise.all([getAllTickets(), getAllReviews()])
      .then(([tickets, reviews]) => {
        if (cancelled) return
        setOpenTickets(tickets.filter((tkt) => !tkt.isClosed).length)
        setTotalReviews(reviews.length)
      })
      .catch(() => {
        /* stat tiles just stay at 0 */
      })
    return () => {
      cancelled = true
    }
  }, [])

  return (
    <section className="account admin-dashboard">
      <h1 className="page-title">{t('admin.dashboardTitle')}</h1>

      <div className="account-grid">
        <Link to="/admin/tickets" className="admin-stat-tile">
          <span className="admin-stat-value">{openTickets}</span>
          <span className="admin-stat-label">{t('admin.openTickets')}</span>
        </Link>
        <Link to="/admin/reviews" className="admin-stat-tile">
          <span className="admin-stat-value">{totalReviews}</span>
          <span className="admin-stat-label">{t('admin.totalReviews')}</span>
        </Link>
      </div>

      <div className="account-grid">
        <Link to="/admin/tickets" className="account-card">
          <h2 className="account-card-title">{t('admin.ticketInbox')}</h2>
        </Link>
        <Link to="/admin/reviews" className="account-card">
          <h2 className="account-card-title">{t('admin.allReviews')}</h2>
        </Link>
      </div>
    </section>
  )
}
