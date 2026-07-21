import { useTranslation } from '../i18n/useTranslation'
import { usePageTitle } from '../hooks/usePageTitle'
import { BrandMark } from '../components/BrandMark'
import { DiscoveryIcon, TrustIcon, AnalysisIcon } from '../components/icons'

const STATS = [
  { icon: DiscoveryIcon, titleKey: 'about.stat1Title', textKey: 'about.stat1Text' },
  { icon: TrustIcon, titleKey: 'about.stat2Title', textKey: 'about.stat2Text' },
  { icon: AnalysisIcon, titleKey: 'about.stat3Title', textKey: 'about.stat3Text' },
] as const

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

      <div className="features-grid about-stats">
        {STATS.map(({ icon: Icon, titleKey, textKey }) => (
          <div className="feature-card" key={titleKey}>
            <span className="feature-icon">
              <Icon />
            </span>
            <h3 className="feature-title">{t(titleKey)}</h3>
            <p className="feature-text">{t(textKey)}</p>
          </div>
        ))}
      </div>

      <section className="transparency">
        <h2 className="transparency-title">{t('about.promiseTitle')}</h2>
        <p className="transparency-text">{t('about.p2')}</p>
      </section>

      <p className="about-name-note">{t('about.p3')}</p>
    </article>
  )
}
