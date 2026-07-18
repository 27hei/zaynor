import { useTranslation } from '../i18n/useTranslation'

const SUGGESTIONS = [
  'Sony PlayStation 5',
  'iPhone 15 Pro',
  'Samsung 55" TV',
  'Nintendo Switch',
  'AirPods Pro',
]

interface PopularSearchesProps {
  onSelect: (query: string) => void
}

/** Suggested searches so a new visitor never faces a blank page (principle J). */
export function PopularSearches({ onSelect }: PopularSearchesProps) {
  const { t } = useTranslation()

  return (
    <div className="popular-searches">
      <span className="popular-searches-label">{t('hero.popularLabel')}</span>
      {SUGGESTIONS.map((query) => (
        <button
          key={query}
          type="button"
          className="popular-chip"
          onClick={() => onSelect(query)}
        >
          {query}
        </button>
      ))}
    </div>
  )
}
