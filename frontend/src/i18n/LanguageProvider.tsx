import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import { LANGUAGES, translations, type Lang } from './translations'
import { LanguageContext } from './LanguageContext'

const STORAGE_KEY = 'zaynor.lang'
const DEFAULT_LANG: Lang = 'ar'

function readInitialLang(): Lang {
  const stored = localStorage.getItem(STORAGE_KEY)
  return stored === 'en' || stored === 'ar' ? stored : DEFAULT_LANG
}

function dirFor(lang: Lang): 'ltr' | 'rtl' {
  return LANGUAGES.find((l) => l.code === lang)?.dir ?? 'ltr'
}

export function LanguageProvider({ children }: { children: ReactNode }) {
  const [lang, setLangState] = useState<Lang>(readInitialLang)

  const dir = dirFor(lang)

  // Keep <html> lang/dir in sync so the whole document mirrors correctly (RTL).
  useEffect(() => {
    const root = document.documentElement
    root.setAttribute('lang', lang)
    root.setAttribute('dir', dir)
  }, [lang, dir])

  const setLang = useCallback((next: Lang) => {
    localStorage.setItem(STORAGE_KEY, next)
    setLangState(next)
  }, [])

  const t = useCallback(
    (key: string, vars?: Record<string, string | number>) => {
      let text = translations[lang][key] ?? translations.en[key] ?? key
      if (vars) {
        for (const [name, value] of Object.entries(vars)) {
          text = text.replace(new RegExp(`\\{${name}\\}`, 'g'), String(value))
        }
      }
      return text
    },
    [lang],
  )

  const value = useMemo(() => ({ lang, dir, setLang, t }), [lang, dir, setLang, t])

  return <LanguageContext.Provider value={value}>{children}</LanguageContext.Provider>
}
