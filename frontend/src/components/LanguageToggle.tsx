import { useTranslation } from '../i18n/useTranslation'

/** Switches between Arabic and English (spec NFR5). Shows the language to switch to. */
export function LanguageToggle() {
  const { lang, setLang } = useTranslation()
  const next = lang === 'ar' ? 'en' : 'ar'
  const label = next === 'ar' ? 'العربية' : 'English'

  return (
    <button
      type="button"
      className="lang-toggle"
      onClick={() => setLang(next)}
      aria-label={`Switch language to ${label}`}
    >
      <span aria-hidden="true">🌐</span>
      {label}
    </button>
  )
}
