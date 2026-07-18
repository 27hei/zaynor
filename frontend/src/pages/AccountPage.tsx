import { useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/useAuth'
import { useTranslation } from '../i18n/useTranslation'

export function AccountPage() {
  const { t, lang } = useTranslation()
  const { user, logout } = useAuth()
  const navigate = useNavigate()

  // Route protection guarantees user is present, but guard for type safety.
  if (!user) return null

  const memberSince = new Intl.DateTimeFormat(lang === 'ar' ? 'ar' : 'en', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
  }).format(new Date(user.createdAt))

  function handleLogout() {
    logout()
    navigate('/')
  }

  return (
    <section className="account">
      <h1 className="page-title">{t('account.title')}</h1>
      <p className="page-subtitle">{t('account.welcome', { email: user.email })}</p>

      <div className="account-panel">
        <dl className="account-details">
          <div className="account-detail">
            <dt>{t('account.emailLabel')}</dt>
            <dd>{user.email}</dd>
          </div>
          <div className="account-detail">
            <dt>{t('account.memberSince')}</dt>
            <dd>{memberSince}</dd>
          </div>
          <div className="account-detail">
            <dt>{t('account.localeLabel')}</dt>
            <dd>{user.locale === 'ar' ? 'العربية' : 'English'}</dd>
          </div>
        </dl>
      </div>

      <div className="account-grid">
        <div className="account-card">
          <h2 className="account-card-title">{t('account.savedTitle')}</h2>
          <span className="account-badge">{t('account.comingSoon')}</span>
          <p className="account-card-text">{t('account.comingSoonText')}</p>
        </div>
        <div className="account-card">
          <h2 className="account-card-title">{t('account.alertsTitle')}</h2>
          <span className="account-badge">{t('account.comingSoon')}</span>
          <p className="account-card-text">{t('account.comingSoonText')}</p>
        </div>
      </div>

      <button type="button" className="btn btn-ghost" onClick={handleLogout}>
        {t('account.logout')}
      </button>
    </section>
  )
}
