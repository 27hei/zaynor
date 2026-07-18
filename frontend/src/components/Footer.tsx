import { Link } from 'react-router-dom'
import { Wordmark } from './Wordmark'
import { useTranslation } from '../i18n/useTranslation'

export function Footer() {
  const { t } = useTranslation()

  return (
    <footer className="footer">
      <div className="footer-inner">
        <div className="footer-brand-col">
          <Wordmark className="footer-brand" />
          <p className="footer-note">{t('footer.note')}</p>
        </div>

        <nav className="footer-col" aria-label={t('footer.product')}>
          <h3 className="footer-col-title">{t('footer.product')}</h3>
          <Link to="/">{t('nav.home')}</Link>
          <Link to="/categories">{t('nav.categories')}</Link>
          <Link to="/how-it-works">{t('nav.howItWorks')}</Link>
        </nav>

        <nav className="footer-col" aria-label={t('footer.company')}>
          <h3 className="footer-col-title">{t('footer.company')}</h3>
          <Link to="/about">{t('nav.about')}</Link>
          <Link to="/register">{t('nav.register')}</Link>
          <Link to="/login">{t('nav.login')}</Link>
        </nav>

        <nav className="footer-col" aria-label={t('footer.legal')}>
          <h3 className="footer-col-title">{t('footer.legal')}</h3>
          <Link to="/privacy">{t('privacy.title')}</Link>
          <Link to="/terms">{t('terms.title')}</Link>
        </nav>
      </div>

      <p className="footer-copyright">{t('footer.rights', { year: new Date().getFullYear() })}</p>
    </footer>
  )
}
