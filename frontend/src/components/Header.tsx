import { Link, NavLink } from 'react-router-dom'
import { BrandMark } from './BrandMark'
import { LanguageToggle } from './LanguageToggle'
import { useTranslation } from '../i18n/useTranslation'
import { useAuth } from '../auth/useAuth'

export function Header() {
  const { t } = useTranslation()
  const { user, logout } = useAuth()

  const navLinkClass = ({ isActive }: { isActive: boolean }) =>
    isActive ? 'nav-link nav-link-active' : 'nav-link'

  return (
    <header className="header">
      <div className="header-inner">
        <Link to="/" className="header-brand" aria-label="Zaynor home">
          <BrandMark size={34} />
          <img src="/zaynor-wordmark.png" alt="Zaynor" className="header-logo-word" />
        </Link>

        <nav className="header-nav" aria-label="Primary">
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
              <Link to="/account" className="btn btn-ghost">
                {t('nav.account')}
              </Link>
              <button type="button" className="btn btn-ghost" onClick={logout}>
                {t('nav.logout')}
              </button>
            </>
          ) : (
            <>
              <Link to="/login" className="btn btn-ghost">
                {t('nav.login')}
              </Link>
              <Link to="/register" className="btn btn-primary">
                {t('nav.register')}
              </Link>
            </>
          )}
        </div>
      </div>
    </header>
  )
}
