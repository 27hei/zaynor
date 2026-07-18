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
      <svg
        width="15"
        height="15"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.7"
        aria-hidden="true"
      >
        <circle cx="12" cy="12" r="9" />
        <path d="M3 12h18M12 3c2.5 2.5 3.8 5.7 3.8 9S14.5 18.5 12 21c-2.5-2.5-3.8-5.7-3.8-9S9.5 5.5 12 3z" />
      </svg>
      <span className="lang-toggle-label">{label}</span>
    </button>
  )
}
