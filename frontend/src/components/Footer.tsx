import { Link } from 'react-router-dom'
import { Wordmark } from './Wordmark'
import { StoreLogo } from './StoreLogo'
import { TRACKED_STORE_NAMES } from '../storeBrand'
import { useTranslation } from '../i18n/useTranslation'

const SUPPORT_EMAIL = 'abdluazez796@gmail.com'

export function Footer() {
  const { t } = useTranslation()

  return (
    <footer className="footer">
      <div className="footer-inner">
        <div className="footer-brand-col">
          <Wordmark className="footer-brand" />
          <p className="footer-note">{t('footer.note')}</p>
          <p className="footer-note">{t('footer.amazonDisclosure')}</p>

          <div className="footer-trust-logos">
            {TRACKED_STORE_NAMES.map((store) => (
              <span key={store} className="footer-trust-logo" title={store}>
                <StoreLogo storeName={store} />
              </span>
            ))}
          </div>
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
          <a href={`mailto:${SUPPORT_EMAIL}`}>{t('footer.contact')}</a>
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
