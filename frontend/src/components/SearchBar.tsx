import { useEffect, useRef, useState, type FormEvent, type KeyboardEvent } from 'react'
import { getSuggestions } from '../api/client'
import { useTranslation } from '../i18n/useTranslation'

const DEBOUNCE_MS = 200
const MIN_CHARS = 2

interface SearchBarProps {
  value: string
  onChange: (value: string) => void
  onSearch: (query: string) => void
  disabled?: boolean
}

/**
 * The search box with live autocomplete (competitive analysis table stakes
 * #1). Suggestions are fetched debounced from the API, navigable by keyboard
 * (arrows/Enter/Escape), and exposed via combobox ARIA roles.
 */
export function SearchBar({ value, onChange, onSearch, disabled }: SearchBarProps) {
  const { t } = useTranslation()
  const [suggestions, setSuggestions] = useState<string[]>([])
  const [open, setOpen] = useState(false)
  const [activeIndex, setActiveIndex] = useState(-1)
  const debounceTimer = useRef<number | undefined>(undefined)
  const fetchController = useRef<AbortController | null>(null)
  const suppressFetch = useRef(false)
  const isFirstRender = useRef(true)

  // Debounced suggestion fetch as the user types.
  useEffect(() => {
    window.clearTimeout(debounceTimer.current)
    fetchController.current?.abort()

    // HomePage swaps in a fresh SearchBar instance (hero → hero-compact) once
    // a search runs, already carrying the committed query as `value`. Without
    // this guard that mount alone would re-fire the fetch and pop the
    // suggestions dropdown, unfocused, over the results (spec NFR: no
    // surprising UI).
    if (isFirstRender.current) {
      isFirstRender.current = false
      return
    }

    if (suppressFetch.current) {
      suppressFetch.current = false
      return
    }

    const trimmed = value.trim()
    if (trimmed.length < MIN_CHARS) {
      setSuggestions([])
      setOpen(false)
      return
    }

    debounceTimer.current = window.setTimeout(async () => {
      const controller = new AbortController()
      fetchController.current = controller
      try {
        const found = await getSuggestions(trimmed, controller.signal)
        // Don't show a dropdown whose only entry is exactly what's typed.
        const useful = found.filter((s) => s.toLowerCase() !== trimmed.toLowerCase())
        setSuggestions(useful)
        setOpen(useful.length > 0)
        setActiveIndex(-1)
      } catch {
        // Aborted or failed — suggestions are best-effort only.
      }
    }, DEBOUNCE_MS)

    return () => window.clearTimeout(debounceTimer.current)
  }, [value])

  function close() {
    setOpen(false)
    setActiveIndex(-1)
  }

  function select(suggestion: string) {
    suppressFetch.current = true
    onChange(suggestion)
    close()
    onSearch(suggestion)
  }

  function handleSubmit(event: FormEvent) {
    event.preventDefault()
    const trimmed = value.trim()
    if (trimmed) {
      close()
      onSearch(trimmed)
    }
  }

  function handleKeyDown(event: KeyboardEvent<HTMLInputElement>) {
    if (!open || suggestions.length === 0) {
      return
    }

    if (event.key === 'ArrowDown') {
      event.preventDefault()
      setActiveIndex((i) => (i + 1) % suggestions.length)
    } else if (event.key === 'ArrowUp') {
      event.preventDefault()
      setActiveIndex((i) => (i <= 0 ? suggestions.length - 1 : i - 1))
    } else if (event.key === 'Enter' && activeIndex >= 0) {
      event.preventDefault()
      select(suggestions[activeIndex])
    } else if (event.key === 'Escape') {
      close()
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
          role="combobox"
          aria-expanded={open}
          aria-controls="search-suggestions"
          aria-autocomplete="list"
          autoComplete="off"
          value={value}
          onChange={(e) => onChange(e.target.value)}
          onKeyDown={handleKeyDown}
          onBlur={() => window.setTimeout(close, 150)}
          disabled={disabled}
        />

        {open && (
          <ul id="search-suggestions" className="search-suggestions" role="listbox">
            {suggestions.map((suggestion, index) => (
              <li
                key={suggestion}
                role="option"
                aria-selected={index === activeIndex}
                className={
                  index === activeIndex
                    ? 'search-suggestion search-suggestion-active'
                    : 'search-suggestion'
                }
                // mousedown fires before the input's blur, so the click wins.
                onMouseDown={(e) => {
                  e.preventDefault()
                  select(suggestion)
                }}
                onMouseEnter={() => setActiveIndex(index)}
              >
                {suggestion}
              </li>
            ))}
          </ul>
        )}
      </div>
      <button type="submit" className="search-button" disabled={disabled}>
        {t('hero.searchButton')}
      </button>
    </form>
  )
}
