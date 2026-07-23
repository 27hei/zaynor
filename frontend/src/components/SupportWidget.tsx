import { Link } from 'react-router-dom'
import { useTranslation } from '../i18n/useTranslation'

/** A persistent, site-wide entry point into customer support — visible from any page. */
export function SupportWidget() {
  const { t } = useTranslation()

  return (
    <Link to="/support" className="support-widget" aria-label={t('support.widgetLabel')}>
      <svg width="40" height="40" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
        <path d="M21 11.5a8.38 8.38 0 0 1-.9 3.8 8.5 8.5 0 0 1-7.6 4.7 8.38 8.38 0 0 1-3.8-.9L3 21l1.9-5.7a8.38 8.38 0 0 1-.9-3.8 8.5 8.5 0 0 1 4.7-7.6 8.38 8.38 0 0 1 3.8-.9h.5a8.48 8.48 0 0 1 8 8v.5z" />
      </svg>
    </Link>
  )
}
