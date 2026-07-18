import { useTranslation } from '../i18n/useTranslation'
import { usePageTitle } from '../hooks/usePageTitle'

const SECTIONS = ['service', 'prices', 'affiliate', 'accounts', 'use', 'liability', 'changes', 'law'] as const

export function TermsPage() {
  const { t } = useTranslation()
  usePageTitle(t('terms.title'))

  return (
    <article className="page-article">
      <h1 className="page-title">{t('terms.title')}</h1>
      <p className="legal-updated">{t('legal.updated')}</p>
      <p>{t('terms.intro')}</p>

      {SECTIONS.map((section) => (
        <section key={section}>
          <h2 className="legal-heading">{t(`terms.${section}.title`)}</h2>
          <p>{t(`terms.${section}.text`)}</p>
        </section>
      ))}
    </article>
  )
}
