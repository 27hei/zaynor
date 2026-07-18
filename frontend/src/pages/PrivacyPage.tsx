import { useTranslation } from '../i18n/useTranslation'
import { usePageTitle } from '../hooks/usePageTitle'

export function PrivacyPage() {
  const { t } = useTranslation()
  usePageTitle(t('privacy.title'))

  return (
    <article className="page-article">
      <h1 className="page-title">{t('privacy.title')}</h1>
      <p>{t('privacy.p1')}</p>
      <p>{t('privacy.p2')}</p>
      <p>{t('privacy.p3')}</p>
    </article>
  )
}
