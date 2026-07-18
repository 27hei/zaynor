import { useTranslation } from '../i18n/useTranslation'
import { usePageTitle } from '../hooks/usePageTitle'

export function AboutPage() {
  const { t } = useTranslation()
  usePageTitle(t('about.title'))

  return (
    <article className="page-article">
      <h1 className="page-title">{t('about.title')}</h1>
      <p>{t('about.p1')}</p>
      <p>{t('about.p2')}</p>
      <p>{t('about.p3')}</p>
    </article>
  )
}
