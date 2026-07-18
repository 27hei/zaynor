import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { getAlerts, getSavedProducts, removeAlert, removeSavedProduct } from '../api/client'
import type { AlertDto, SavedProductDto } from '../api/types'
import { useAuth } from '../auth/useAuth'
import { useTranslation } from '../i18n/useTranslation'
import { usePageTitle } from '../hooks/usePageTitle'

export function AccountPage() {
  const { t, lang } = useTranslation()
  const { user, logout } = useAuth()
  const navigate = useNavigate()

  const [savedProducts, setSavedProducts] = useState<SavedProductDto[]>([])
  const [alerts, setAlerts] = useState<AlertDto[]>([])
  const [itemsError, setItemsError] = useState(false)

  usePageTitle(t('nav.account'))

  useEffect(() => {
    let cancelled = false

    Promise.all([getSavedProducts(), getAlerts()])
      .then(([savedList, alertList]) => {
        if (cancelled) return
        setSavedProducts(savedList)
        setAlerts(alertList)
      })
      .catch(() => {
        if (!cancelled) setItemsError(true)
      })

    return () => {
      cancelled = true
    }
  }, [])

  // Route protection guarantees user is present, but guard for type safety.
  if (!user) return null

  const dateFormat = new Intl.DateTimeFormat(lang === 'ar' ? 'ar' : 'en', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
  })

  async function handleRemoveSaved(id: number) {
    try {
      await removeSavedProduct(id)
      setSavedProducts((current) => current.filter((s) => s.id !== id))
    } catch {
      setItemsError(true)
    }
  }

  async function handleRemoveAlert(id: number) {
    try {
      await removeAlert(id)
      setAlerts((current) => current.filter((a) => a.id !== id))
    } catch {
      setItemsError(true)
    }
  }

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
            <dd>{dateFormat.format(new Date(user.createdAt))}</dd>
          </div>
          <div className="account-detail">
            <dt>{t('account.localeLabel')}</dt>
            <dd>{user.locale === 'ar' ? 'العربية' : 'English'}</dd>
          </div>
        </dl>
      </div>

      {itemsError && (
        <p className="hint hint-error" role="alert">
          {t('account.loadError')}
        </p>
      )}

      <div className="account-grid">
        <div className="account-card">
          <h2 className="account-card-title">{t('account.savedTitle')}</h2>
          {savedProducts.length === 0 ? (
            <p className="account-card-text">{t('account.emptySaved')}</p>
          ) : (
            <ul className="item-list">
              {savedProducts.map((item) => (
                <li className="item-row" key={item.id}>
                  <div className="item-info">
                    <span className="item-name">{item.productName}</span>
                    <span className="item-date">{dateFormat.format(new Date(item.savedAt))}</span>
                  </div>
                  <button
                    type="button"
                    className="item-remove"
                    onClick={() => handleRemoveSaved(item.id)}
                  >
                    {t('account.remove')}
                  </button>
                </li>
              ))}
            </ul>
          )}
        </div>

        <div className="account-card">
          <h2 className="account-card-title">{t('account.alertsTitle')}</h2>
          {alerts.length === 0 ? (
            <p className="account-card-text">{t('account.emptyAlerts')}</p>
          ) : (
            <>
              <ul className="item-list">
                {alerts.map((alert) => (
                  <li className="item-row" key={alert.id}>
                    <div className="item-info">
                      <span className="item-name">{alert.productName}</span>
                      <span className="item-date">
                        {dateFormat.format(new Date(alert.createdAt))}
                      </span>
                    </div>
                    <button
                      type="button"
                      className="item-remove"
                      onClick={() => handleRemoveAlert(alert.id)}
                    >
                      {t('account.remove')}
                    </button>
                  </li>
                ))}
              </ul>
              <p className="account-note">{t('account.alertNote')}</p>
            </>
          )}
        </div>
      </div>

      <button type="button" className="btn btn-ghost" onClick={handleLogout}>
        {t('account.logout')}
      </button>
    </section>
  )
}
