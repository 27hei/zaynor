import { useEffect, useRef, useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { useTranslation } from '../i18n/useTranslation'

/**
 * A compact, icon-triggered search that lives in the header on every page
 * except Home (which already has its own prominent search box) — so a user
 * reading About or Categories can search without navigating back first.
 * Submitting runs the same real search as the homepage via the ?q= param it
 * already reads on mount.
 */
export function HeaderSearch() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const [open, setOpen] = useState(false)
  const [value, setValue] = useState('')
  const inputRef = useRef<HTMLInputElement>(null)

  useEffect(() => {
    if (open) inputRef.current?.focus()
  }, [open])

  function handleSubmit(event: FormEvent) {
    event.preventDefault()
    const trimmed = value.trim()
    if (!trimmed) return
    navigate(`/?q=${encodeURIComponent(trimmed)}`)
    setValue('')
    setOpen(false)
  }

  if (!open) {
    return (
      <button
        type="button"
        className="header-search-toggle"
        aria-label={t('hero.searchPlaceholder')}
        onClick={() => setOpen(true)}
      >
        <svg width="19" height="19" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" aria-hidden="true">
          <circle cx="11" cy="11" r="7" />
          <path d="m21 21-4.3-4.3" />
        </svg>
      </button>
    )
  }

  return (
    <form className="header-search" onSubmit={handleSubmit} role="search">
      <svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" aria-hidden="true">
        <circle cx="11" cy="11" r="7" />
        <path d="m21 21-4.3-4.3" />
      </svg>
      <input
        ref={inputRef}
        type="search"
        value={value}
        onChange={(e) => setValue(e.target.value)}
        onBlur={() => window.setTimeout(() => { if (!value) setOpen(false) }, 150)}
        placeholder={t('hero.searchPlaceholder')}
        aria-label={t('hero.searchPlaceholder')}
      />
    </form>
  )
}
