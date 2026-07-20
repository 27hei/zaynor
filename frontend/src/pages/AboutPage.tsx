import { useTranslation } from '../i18n/useTranslation'
import { usePageTitle } from '../hooks/usePageTitle'
import { BrandMark } from '../components/BrandMark'

export function AboutPage() {
  const { t } = useTranslation()
  usePageTitle(t('about.title'))

  return (
    <article className="page-article">
      <div className="about-hero">
        <BrandMark size={56} detailed />
        <div>
          <h1 className="page-title">{t('about.title')}</h1>
          <p className="page-subtitle">{t('about.p1')}</p>
        </div>
      </div>

      <section className="transparency">
        <h2 className="transparency-title">{t('about.promiseTitle')}</h2>
        <p className="transparency-text">{t('about.p2')}</p>
      </section>

      <p className="about-name-note">{t('about.p3')}</p>
    </article>
  )
}
