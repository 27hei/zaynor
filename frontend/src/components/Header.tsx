import { useEffect, useState } from 'react'
import { Link, NavLink, useLocation } from 'react-router-dom'
import { BrandMark } from './BrandMark'
import { LanguageToggle } from './LanguageToggle'
import { ThemeToggle } from './ThemeToggle'
import { HeaderSearch } from './HeaderSearch'
import { HeartIcon, BellIcon } from './icons'
import { getSavedProducts, getAlerts } from '../api/client'
import { useTranslation } from '../i18n/useTranslation'
import { useAuth } from '../auth/useAuth'

export function Header() {
  const { t } = useTranslation()
  const { user, logout } = useAuth()
  const location = useLocation()
  const [menuOpen, setMenuOpen] = useState(false)
  const [savedCount, setSavedCount] = useState(0)
  const [activeAlertCount, setActiveAlertCount] = useState(0)
  const [scrolled, setScrolled] = useState(false)

  const navLinkClass = ({ isActive }: { isActive: boolean }) =>
    isActive ? 'nav-link nav-link-active' : 'nav-link'

  const close = () => setMenuOpen(false)

  // Shrinks the header once the page has scrolled a little, giving content
  // more room without hiding the header entirely.
  useEffect(() => {
    function onScroll() {
      setScrolled(window.scrollY > 12)
    }
    onScroll()
    window.addEventListener('scroll', onScroll, { passive: true })
    return () => window.removeEventListener('scroll', onScroll)
  }, [])

  // Powers the wishlist-style saved-products shortcut; re-checked on
  // navigation so the badge stays right after saving/removing a product
  // elsewhere.
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

  // The bell's badge is a real count of active price-drop alerts (FR8) —
  // never a placeholder number.
  useEffect(() => {
    if (!user) {
      setActiveAlertCount(0)
      return
    }
    let cancelled = false
    getAlerts()
      .then((list) => {
        if (!cancelled) setActiveAlertCount(list.filter((a) => a.isActive).length)
      })
      .catch(() => {
        if (!cancelled) setActiveAlertCount(0)
      })
    return () => {
      cancelled = true
    }
  }, [user, location.pathname])

  const initial = user?.email.charAt(0).toUpperCase()

  const headerClass = ['header', menuOpen && 'menu-open', scrolled && 'header-scrolled']
    .filter(Boolean)
    .join(' ')

  return (
    <header className={headerClass}>
      <div className="header-inner">
        {/* A plain <a>, not <Link> — a full page reload back to a clean
            home state is the point here, not client-side navigation. */}
        <a href="/" className="header-brand" aria-label="Zaynor home">
          <BrandMark size={scrolled ? 28 : 34} />
          <img src="/zaynor-wordmark.png" alt="Zaynor" className="header-logo-word" />
        </a>

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
          {user?.isAdmin && (
            <NavLink to="/admin" className={navLinkClass}>
              {t('nav.admin')}
            </NavLink>
          )}
        </nav>

        <div className="header-actions">
          {location.pathname !== '/' && location.pathname !== '/product' && <HeaderSearch />}

          {user && (
            <Link to="/account" className="header-cart" aria-label={t('account.savedTitle')} onClick={close}>
              <HeartIcon />
              {savedCount > 0 && <span className="header-cart-badge">{savedCount}</span>}
            </Link>
          )}

          {user && (
            <Link to="/account" className="header-cart" aria-label={t('account.alertsTitle')} onClick={close}>
              <BellIcon />
              {activeAlertCount > 0 && <span className="header-cart-badge">{activeAlertCount}</span>}
            </Link>
          )}

          <ThemeToggle />
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
