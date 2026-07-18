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
      <input
        type="search"
        className="search-input"
        placeholder={t('hero.searchPlaceholder')}
        aria-label={t('hero.searchPlaceholder')}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        disabled={disabled}
      />
      <button type="submit" className="search-button" disabled={disabled}>
        {t('hero.searchButton')}
      </button>
    </form>
  )
}
