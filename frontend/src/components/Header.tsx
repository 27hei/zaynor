import { useState } from 'react'
import { Link, NavLink } from 'react-router-dom'
import { BrandMark } from './BrandMark'
import { LanguageToggle } from './LanguageToggle'
import { useTranslation } from '../i18n/useTranslation'
import { useAuth } from '../auth/useAuth'

export function Header() {
  const { t } = useTranslation()
  const { user, logout } = useAuth()
  const [menuOpen, setMenuOpen] = useState(false)

  const navLinkClass = ({ isActive }: { isActive: boolean }) =>
    isActive ? 'nav-link nav-link-active' : 'nav-link'

  const close = () => setMenuOpen(false)

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
          <LanguageToggle />
          {user ? (
            <>
              <Link to="/account" className="btn btn-ghost" onClick={close}>
                {t('nav.account')}
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
