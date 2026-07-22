import { StoreLogo } from './StoreLogo'
import { useTranslation } from '../i18n/useTranslation'

/**
 * A decorative illustration of the site inside a phone frame — hero art,
 * not a live view. The store logos are real (StoreLogo/STORE_BRAND), but the
 * product/price shown is a static example, like any marketing screenshot.
 */
export function PhoneMockup() {
  const { t } = useTranslation()

  return (
    <div className="phone-mockup" aria-hidden="true">
      <div className="phone-mockup-notch" />
      <div className="phone-mockup-screen">
        <div className="phone-mockup-topbar">
          <span className="phone-mockup-brand">ZAYNOR</span>
          <span className="phone-mockup-chevron">›</span>
        </div>

        <div className="phone-mockup-search">
          <span>{t('phoneMockup.searchPlaceholder')}</span>
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
            <circle cx="10.5" cy="10.5" r="6.5" />
            <path d="m19.5 19.5-4-4" />
          </svg>
        </div>

        <p className="phone-mockup-label">🔥 {t('phoneMockup.dealOfDay')}</p>

        <div className="phone-mockup-card">
          <div className="phone-mockup-card-image">
            <img src="/product-art/phone.svg" alt="" aria-hidden="true" />
          </div>
          <div className="phone-mockup-card-info">
            <span className="phone-mockup-card-title">iPhone 15 Pro Max 256GB</span>
            <span className="phone-mockup-card-price-row">
              <span className="phone-mockup-card-price">4,299 {t('currency.sar')}</span>
              <span className="phone-mockup-card-price-old">4,999 {t('currency.sar')}</span>
            </span>
            <span className="phone-mockup-card-save">{t('phoneMockup.saveAmount', { amount: '700' })}</span>
            <span className="phone-mockup-card-seller">
              <StoreLogo storeName="Amazon.sa" />
              <span>{t('phoneMockup.seller')}</span>
              <span className="phone-mockup-card-rating">★ 4.8</span>
            </span>
            <span className="phone-mockup-card-cta">{t('phoneMockup.viewDeal')}</span>
          </div>
        </div>

        <p className="phone-mockup-label">{t('phoneMockup.trustedStores')}</p>
        <div className="phone-mockup-stores">
          {['Amazon.sa', 'Noon', 'Extra', 'Jarir'].map((name) => (
            <span key={name} className="phone-mockup-store" title={name}>
              <StoreLogo storeName={name} />
            </span>
          ))}
        </div>
      </div>
    </div>
  )
}
