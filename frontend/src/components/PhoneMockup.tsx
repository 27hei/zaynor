import { useState } from 'react'
import { useTranslation } from '../i18n/useTranslation'

/**
 * A decorative preview of the Zaynor app inside a 3D phone frame — hero art,
 * not a live view. The "screen" is a cropped still from the approved design
 * reference (an illustration, like any marketing screenshot), animated
 * between two crops to suggest scrolling; captions fade in sync. Clicking
 * pauses the motion (mirrors the original template's app.js behavior).
 */
export function PhoneMockup() {
  const { t } = useTranslation()
  const [paused, setPaused] = useState(false)

  return (
    <div className="phone-side" aria-label={t('phoneMockup.previewLabel')}>
      <div className={paused ? 'phone-3d is-paused' : 'phone-3d'} role="group" aria-label={t('phoneMockup.previewLabel')}>
        <button
          type="button"
          className="phone-screen"
          aria-pressed={paused}
          aria-label={paused ? t('phoneMockup.resumeMotion') : t('phoneMockup.pauseMotion')}
          onClick={() => setPaused((v) => !v)}
        >
          <span className="phone-live-status" aria-hidden="true">
            <span className="phone-state state-search">
              <i /> {t('phoneMockup.stateSearch')}
            </span>
            <span className="phone-state state-found">
              <i /> {t('phoneMockup.stateFound')}
            </span>
            <span className="phone-state state-save">
              <i /> {t('phoneMockup.stateSave')}
            </span>
          </span>
          <span className="price-alert" aria-hidden="true">
            <b>↓ 12%</b> {t('phoneMockup.priceAlert')}
          </span>
          <span className="screen-hint" aria-hidden="true">
            {paused ? t('phoneMockup.resumeMotion') : t('phoneMockup.pauseMotion')}
          </span>
        </button>
      </div>
      <aside className="guarantee-card">
        <span className="shield">✓</span>
        <p>{t('phoneMockup.guaranteeWe')}</p>
        <strong>{t('phoneMockup.guaranteeBest')}</strong>
        <p>{t('phoneMockup.guaranteeFrom')}</p>
      </aside>
    </div>
  )
}
