import { Logo } from './Logo'
import { Wordmark } from './Wordmark'
import { useTranslation } from '../i18n/useTranslation'

/** The full brand mark for expressive placements: icon, wordmark, tagline. */
export function BrandLockup() {
  const { t } = useTranslation()

  return (
    <div className="brand-lockup">
      <Logo size={72} detailed />
      <Wordmark className="brand-lockup-wordmark" />
      <p className="brand-lockup-tagline">
        <span className="tagline-line" aria-hidden="true" />
        {t('brand.tagline')}
        <span className="tagline-line" aria-hidden="true" />
      </p>
    </div>
  )
}
