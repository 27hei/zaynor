import { useTranslation } from '../i18n/useTranslation'

/** A thin site-wide announcement strip above the header. */
export function PromoBar() {
  const { t } = useTranslation()

  return (
    <div className="announcement" aria-label={t('promo.ariaLabel')}>
      <div className="announcement-inner">
        <strong>🔥 {t('promo.title')}</strong>
        <span>
          {t('promo.subtitle')} <b>✨</b>
        </span>
      </div>
    </div>
  )
}
