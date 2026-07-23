import { outboundUrl } from '../api/client'
import { useTranslation } from '../i18n/useTranslation'
import { StoreLogo } from './StoreLogo'
import { SavingsIcon } from './icons'

const NOON_TRACKING_URL = 'https://s.noon.com/dBcC-E2fLJ8'

/**
 * A standing, always-visible Noon promo on the homepage (not tied to any one
 * search) using the real campaign tracking link from Noon's partner
 * dashboard, so every click is attributed regardless of what a visitor
 * searches for. Rides through /api/out for click-count stats, same as every
 * other outbound link.
 */
export function NoonHomeBanner() {
  const { t } = useTranslation()

  return (
    <a
      className="noon-home-banner"
      href={outboundUrl(NOON_TRACKING_URL, 'Noon', 'homepage-banner')}
      target="_blank"
      rel="noopener noreferrer sponsored"
    >
      <span className="noon-home-banner-store">
        <StoreLogo storeName="Noon" />
      </span>
      <span className="noon-home-banner-copy">
        <strong>{t('noonBanner.title')}</strong>
        <span>{t('noonBanner.subtitle')}</span>
      </span>
      <span className="noon-home-banner-cta">
        <SavingsIcon />
        {t('noonBanner.cta')}
      </span>
    </a>
  )
}
