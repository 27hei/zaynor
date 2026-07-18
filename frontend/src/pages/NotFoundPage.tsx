import { Link } from 'react-router-dom'
import { useTranslation } from '../i18n/useTranslation'
import { usePageTitle } from '../hooks/usePageTitle'

export function NotFoundPage() {
  const { t } = useTranslation()
  usePageTitle(t('notFound.title'))

  return (
    <section className="not-found">
      <h1 className="page-title">{t('notFound.title')}</h1>
      <p className="page-subtitle">{t('notFound.text')}</p>
      <Link to="/" className="btn btn-primary">
        {t('notFound.home')}
      </Link>
    </section>
  )
}
