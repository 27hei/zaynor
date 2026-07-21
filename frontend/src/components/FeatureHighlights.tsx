import {
  AlertsIcon,
  AnalysisIcon,
  DiscoveryIcon,
  IntelligenceIcon,
  SavingsIcon,
  TrustIcon,
} from './icons'
import { Reveal } from './Reveal'
import { useTranslation } from '../i18n/useTranslation'

const FEATURES = [
  { icon: TrustIcon, key: 'trust' },
  { icon: IntelligenceIcon, key: 'intelligence' },
  { icon: SavingsIcon, key: 'savings' },
  { icon: DiscoveryIcon, key: 'discovery' },
  { icon: AnalysisIcon, key: 'analysis' },
  { icon: AlertsIcon, key: 'alerts' },
] as const

export function FeatureHighlights() {
  const { t } = useTranslation()

  return (
    <section className="features" aria-label={t('feature.trust.title')}>
      <div className="features-grid">
        {FEATURES.map(({ icon: Icon, key }, i) => (
          <Reveal key={key} delayMs={i * 60}>
            <div className="feature-card">
              <div className="feature-icon">
                <Icon />
              </div>
              <h3 className="feature-title">{t(`feature.${key}.title`)}</h3>
              <p className="feature-text">{t(`feature.${key}.text`)}</p>
            </div>
          </Reveal>
        ))}
      </div>
    </section>
  )
}
