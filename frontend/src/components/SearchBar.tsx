import type { FormEvent } from 'react'
import { useTranslation } from '../i18n/useTranslation'

interface SearchBarProps {
  value: string
  onChange: (value: string) => void
  onSearch: (query: string) => void
  disabled?: boolean
}

export function SearchBar({ value, onChange, onSearch, disabled }: SearchBarProps) {
  const { t } = useTranslation()

  function handleSubmit(event: FormEvent) {
    event.preventDefault()
    const trimmed = value.trim()
    if (trimmed) {
      onSearch(trimmed)
    }
  }

  return (
    <form className="search-bar" onSubmit={handleSubmit} role="search">
      <div className="search-field">
        <svg
          className="search-field-icon"
          width="20"
          height="20"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          strokeWidth="2"
          strokeLinecap="round"
          aria-hidden="true"
        >
          <circle cx="11" cy="11" r="7" />
          <path d="m21 21-4.3-4.3" />
        </svg>
        <input
          type="search"
          className="search-input"
          placeholder={t('hero.searchPlaceholder')}
          aria-label={t('hero.searchPlaceholder')}
          value={value}
          onChange={(e) => onChange(e.target.value)}
          disabled={disabled}
        />
      </div>
      <button type="submit" className="search-button" disabled={disabled}>
        {t('hero.searchButton')}
      </button>
    </form>
  )
}
