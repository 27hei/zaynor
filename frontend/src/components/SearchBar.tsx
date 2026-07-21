import { useEffect, useRef, useState, type FormEvent, type KeyboardEvent } from 'react'
import { getSuggestions } from '../api/client'
import { useTranslation } from '../i18n/useTranslation'

const DEBOUNCE_MS = 200
const MIN_CHARS = 2

// The Web Speech API is real, browser-native, and free — no server round
// trip or third-party key needed. It isn't standardized (no lib.dom types,
// no Firefox support), so it's read once, loosely typed, and the mic button
// simply doesn't render where it's unavailable (progressive enhancement).
type SpeechRecognitionLike = {
  lang: string
  interimResults: boolean
  maxAlternatives: number
  start: () => void
  stop: () => void
  onresult: ((event: { results: { [i: number]: { [j: number]: { transcript: string } } } }) => void) | null
  onerror: (() => void) | null
  onend: (() => void) | null
}
const SpeechRecognitionCtor: (new () => SpeechRecognitionLike) | undefined =
  typeof window !== 'undefined'
    ? ((window as unknown as Record<string, unknown>).SpeechRecognition as new () => SpeechRecognitionLike) ??
      ((window as unknown as Record<string, unknown>).webkitSpeechRecognition as new () => SpeechRecognitionLike)
    : undefined

interface SearchBarProps {
  value: string
  onChange: (value: string) => void
  onSearch: (query: string) => void
  disabled?: boolean
  recentSearches?: string[]
  onClearRecent?: () => void
}

/**
 * The search box with live autocomplete (competitive analysis table stakes
 * #1). Below MIN_CHARS the dropdown shows recent searches instead (opened on
 * focus, like a favorites list); at MIN_CHARS+ it switches to live API
 * suggestions. Both are navigable by keyboard (arrows/Enter/Escape) and
 * exposed via combobox ARIA roles.
 */
export function SearchBar({
  value,
  onChange,
  onSearch,
  disabled,
  recentSearches = [],
  onClearRecent,
}: SearchBarProps) {
  const { t, lang } = useTranslation()
  const [suggestions, setSuggestions] = useState<string[]>([])
  const [open, setOpen] = useState(false)
  const [activeIndex, setActiveIndex] = useState(-1)
  const [listening, setListening] = useState(false)
  const debounceTimer = useRef<number | undefined>(undefined)
  const fetchController = useRef<AbortController | null>(null)
  const suppressFetch = useRef(false)
  const isFirstRender = useRef(true)
  const recognitionRef = useRef<SpeechRecognitionLike | null>(null)

  useEffect(() => () => recognitionRef.current?.stop(), [])

  function toggleVoiceSearch() {
    if (!SpeechRecognitionCtor) return
    if (listening) {
      recognitionRef.current?.stop()
      return
    }
    const recognition = new SpeechRecognitionCtor()
    recognition.lang = lang === 'ar' ? 'ar-SA' : 'en-US'
    recognition.interimResults = false
    recognition.maxAlternatives = 1
    recognition.onresult = (event) => {
      const transcript = event.results[0][0].transcript
      onChange(transcript)
      onSearch(transcript)
    }
    recognition.onerror = () => setListening(false)
    recognition.onend = () => setListening(false)
    recognitionRef.current = recognition
    setListening(true)
    recognition.start()
  }

  const isShort = value.trim().length < MIN_CHARS
  const items = isShort ? recentSearches : suggestions

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
      setActiveIndex(-1)
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

  function handleFocus() {
    // Below MIN_CHARS there's nothing to fetch — offer recent searches
    // instead, like a favorites list, so the box is never a dead end.
    if (isShort && recentSearches.length > 0) {
      setOpen(true)
    }
  }

  function handleSubmit(event: FormEvent) {
    event.preventDefault()
    const trimmed = value.trim()
    if (trimmed) {
      // Cancel any in-flight/pending suggestion fetch — otherwise it can
      // resolve after submit and reopen the dropdown on top of the results
      // that just rendered underneath.
      window.clearTimeout(debounceTimer.current)
      fetchController.current?.abort()
      close()
      onSearch(trimmed)
    }
  }

  function handleKeyDown(event: KeyboardEvent<HTMLInputElement>) {
    if (!open || items.length === 0) {
      return
    }

    if (event.key === 'ArrowDown') {
      event.preventDefault()
      setActiveIndex((i) => (i + 1) % items.length)
    } else if (event.key === 'ArrowUp') {
      event.preventDefault()
      setActiveIndex((i) => (i <= 0 ? items.length - 1 : i - 1))
    } else if (event.key === 'Enter' && activeIndex >= 0) {
      event.preventDefault()
      select(items[activeIndex])
    } else if (event.key === 'Escape') {
      close()
    }
  }

  const showDropdown = open && items.length > 0

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
          aria-expanded={showDropdown}
          aria-controls="search-suggestions"
          aria-autocomplete="list"
          autoComplete="off"
          value={value}
          onChange={(e) => onChange(e.target.value)}
          onKeyDown={handleKeyDown}
          onFocus={handleFocus}
          onBlur={() => window.setTimeout(close, 150)}
          disabled={disabled}
        />

        {SpeechRecognitionCtor && (
          <button
            type="button"
            className={listening ? 'search-mic search-mic-active' : 'search-mic'}
            aria-label={listening ? t('hero.listening') : t('hero.voiceSearch')}
            aria-pressed={listening}
            onClick={toggleVoiceSearch}
            disabled={disabled}
          >
            <svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" aria-hidden="true">
              <rect x="9" y="2" width="6" height="12" rx="3" />
              <path d="M5 10a7 7 0 0 0 14 0" />
              <path d="M12 19v3" />
            </svg>
          </button>
        )}

        {showDropdown && (
          <ul id="search-suggestions" className="search-suggestions" role="listbox">
            {isShort && (
              <li className="search-suggestions-heading">
                <span>{t('hero.recentLabel')}</span>
                {onClearRecent && (
                  <button
                    type="button"
                    className="recent-clear"
                    onMouseDown={(e) => {
                      e.preventDefault()
                      onClearRecent()
                      close()
                    }}
                  >
                    {t('hero.clearRecent')}
                  </button>
                )}
              </li>
            )}
            {items.map((item, index) => (
              <li
                key={item}
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
                  select(item)
                }}
                onMouseEnter={() => setActiveIndex(index)}
              >
                {item}
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
