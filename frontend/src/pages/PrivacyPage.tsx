import { useTranslation } from '../i18n/useTranslation'
import { usePageTitle } from '../hooks/usePageTitle'

const SECTIONS = ['collect', 'use', 'local', 'affiliate', 'sharing', 'security', 'rights', 'changes'] as const

export function PrivacyPage() {
  const { t } = useTranslation()
  usePageTitle(t('privacy.title'))

  return (
    <article className="page-article">
      <h1 className="page-title">{t('privacy.title')}</h1>
      <p className="legal-updated">{t('legal.updated')}</p>
      <p>{t('privacy.intro')}</p>

      {SECTIONS.map((section) => (
        <section key={section}>
          <h2 className="legal-heading">{t(`privacy.${section}.title`)}</h2>
          <p>{t(`privacy.${section}.text`)}</p>
        </section>
      ))}
    </article>
  )
}
