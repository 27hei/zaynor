import { useEffect, useState } from 'react'
import { Link, NavLink, useLocation } from 'react-router-dom'
import { BrandMark } from './BrandMark'
import { LanguageToggle } from './LanguageToggle'
import { HeaderSearch } from './HeaderSearch'
import { CartIcon } from './icons'
import { getSavedProducts } from '../api/client'
import { useTranslation } from '../i18n/useTranslation'
import { useAuth } from '../auth/useAuth'

export function Header() {
  const { t } = useTranslation()
  const { user, logout } = useAuth()
  const location = useLocation()
  const [menuOpen, setMenuOpen] = useState(false)
  const [savedCount, setSavedCount] = useState(0)

  const navLinkClass = ({ isActive }: { isActive: boolean }) =>
    isActive ? 'nav-link nav-link-active' : 'nav-link'

  const close = () => setMenuOpen(false)

  // Powers the cart-style saved-products shortcut; re-checked on navigation
  // so the badge stays right after saving/removing a product elsewhere.
  useEffect(() => {
    if (!user) {
      setSavedCount(0)
      return
    }
    let cancelled = false
    getSavedProducts()
      .then((list) => {
        if (!cancelled) setSavedCount(list.length)
      })
      .catch(() => {
        if (!cancelled) setSavedCount(0)
      })
    return () => {
      cancelled = true
    }
  }, [user, location.pathname])

  const initial = user?.email.charAt(0).toUpperCase()

  return (
    <header className={menuOpen ? 'header menu-open' : 'header'}>
      <div className="header-inner">
        <Link to="/" className="header-brand" aria-label="Zaynor home" onClick={close}>
          <BrandMark size={34} />
          <img src="/zaynor-wordmark.png" alt="Zaynor" className="header-logo-word" />
        </Link>

        {/* Mobile menu toggle (hidden on desktop via CSS) */}
        <button
          type="button"
          className="menu-toggle"
          aria-label={t('nav.menu')}
          aria-expanded={menuOpen}
          onClick={() => setMenuOpen((v) => !v)}
        >
          <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" aria-hidden="true">
            {menuOpen ? <path d="M6 6l12 12M18 6L6 18" /> : <path d="M4 7h16M4 12h16M4 17h16" />}
          </svg>
        </button>

        <nav className="header-nav" aria-label="Primary" onClick={close}>
          <NavLink to="/" className={navLinkClass} end>
            {t('nav.home')}
          </NavLink>
          <NavLink to="/categories" className={navLinkClass}>
            {t('nav.categories')}
          </NavLink>
          <NavLink to="/how-it-works" className={navLinkClass}>
            {t('nav.howItWorks')}
          </NavLink>
          <NavLink to="/about" className={navLinkClass}>
            {t('nav.about')}
          </NavLink>
        </nav>

        <div className="header-actions">
          {location.pathname !== '/' && <HeaderSearch />}

          {user && (
            <Link to="/account" className="header-cart" aria-label={t('account.savedTitle')} onClick={close}>
              <CartIcon />
              {savedCount > 0 && <span className="header-cart-badge">{savedCount}</span>}
            </Link>
          )}

          <LanguageToggle />
          {user ? (
            <>
              <Link to="/account" className="header-avatar" onClick={close} aria-label={t('nav.account')}>
                {initial}
              </Link>
              <button
                type="button"
                className="btn btn-ghost"
                onClick={() => {
                  close()
                  logout()
                }}
              >
                {t('nav.logout')}
              </button>
            </>
          ) : (
            <>
              <Link to="/login" className="btn btn-ghost" onClick={close}>
                {t('nav.login')}
              </Link>
              <Link to="/register" className="btn btn-primary" onClick={close}>
                {t('nav.register')}
              </Link>
            </>
          )}
        </div>
      </div>
    </header>
  )
}
