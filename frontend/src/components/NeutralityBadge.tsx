import { TrustIcon } from './icons'
import { useTranslation } from '../i18n/useTranslation'

/**
 * A loud, upfront neutrality signal (competitive analysis Section 3.2 / 6.D):
 * most comparison sites quietly favor higher-commission stores. Zaynor's
 * spec explicitly commits to ranking by price alone (Section 6), so this
 * promise is real, not marketing — safe to state plainly.
 */
export function NeutralityBadge() {
  const { t } = useTranslation()

  return (
    <p className="neutrality-badge">
      <TrustIcon className="neutrality-icon" />
      {t('hero.neutrality')}
    </p>
  )
}
