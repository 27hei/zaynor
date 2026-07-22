import { useTranslation } from '../i18n/useTranslation'

/** A thin site-wide announcement strip above the header. */
export function PromoBar() {
  const { t } = useTranslation()

  return (
    <div className="promo-bar" role="note">
      <span>✨ {t('promo.banner')} 🔥</span>
    </div>
  )
}
