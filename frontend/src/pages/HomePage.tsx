import { useEffect, useRef, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { searchProducts } from '../api/client'
import type { SearchResult } from '../api/types'
import { Logo } from '../components/Logo'
import { SearchBar } from '../components/SearchBar'
import { NeutralityBadge } from '../components/NeutralityBadge'
import { PopularSearches } from '../components/PopularSearches'
import { RecommendationBanner } from '../components/RecommendationBanner'
import { OfferList } from '../components/OfferList'
import { OfferListSkeleton } from '../components/OfferListSkeleton'
import { FeatureHighlights } from '../components/FeatureHighlights'
import { useTranslation } from '../i18n/useTranslation'

const TRACKED_STORES = ['Amazon.sa', 'Noon', 'Jarir', 'Extra', 'AliExpress']

export function HomePage() {
  const { t } = useTranslation()
  const [searchParams] = useSearchParams()
  const [query, setQuery] = useState('')
  const [result, setResult] = useState<SearchResult | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const activeRequest = useRef<AbortController | null>(null)
  const consumedInitialQuery = useRef(false)

  async function handleSearch(searchQuery: string) {
    activeRequest.current?.abort()
    const controller = new AbortController()
    activeRequest.current = controller

    setQuery(searchQuery)
    setLoading(true)
    setError(null)

    try {
      const data = await searchProducts(searchQuery, controller.signal)
      setResult(data)
    } catch (err) {
      if ((err as Error).name === 'AbortError') return
      setError((err as Error).message)
      setResult(null)
    } finally {
      if (activeRequest.current === controller) {
        setLoading(false)
      }
    }
  }

  // Allow deep-linking a search, e.g. category cards link to /?q=iPhone 15.
  useEffect(() => {
    if (consumedInitialQuery.current) return
    consumedInitialQuery.current = true
    const initial = searchParams.get('q')
    if (initial) {
      setQuery(initial)
      handleSearch(initial)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const hasSearched = result !== null
  const hasResults = !!result && result.offers.length > 0

  const statusMessage = loading
    ? t('results.searching')
    : hasSearched
      ? hasResults
        ? t('results.heading', { count: result!.offerCount, query: result!.query })
        : t('results.noResults', { query: result!.query })
      : ''

  return (
    <>
      {!hasSearched ? (
        <section className="hero">
          <div className="hero-mark">
            <Logo size={58} detailed />
          </div>
          <p className="hero-eyebrow">{t('hero.eyebrow')}</p>
          <h1 className="hero-title">{t('hero.title')}</h1>
          <p className="hero-subtitle">{t('hero.subtitle')}</p>

          <SearchBar value={query} onChange={setQuery} onSearch={handleSearch} disabled={loading} />
          <NeutralityBadge />
          <PopularSearches onSelect={handleSearch} />

          <p className="hero-trust">
            {t('hero.trustLine')}{' '}
            {TRACKED_STORES.map((store, i) => (
              <span key={store}>
                <span className="hero-trust-store">{store}</span>
                {i < TRACKED_STORES.length - 1 && <span> · </span>}
              </span>
            ))}
          </p>
        </section>
      ) : (
        <section className="hero hero-compact">
          <SearchBar value={query} onChange={setQuery} onSearch={handleSearch} disabled={loading} />
        </section>
      )}

      <p className="sr-only" role="status" aria-live="polite">
        {statusMessage}
      </p>

      {error && (
        <p className="hint hint-error" role="alert">
          {t('results.error', { message: error })}
        </p>
      )}

      {loading && (
        <section className="results" aria-label={t('results.searching')} aria-busy="true">
          <p className="hint">{t('results.searching')}</p>
          <OfferListSkeleton />
        </section>
      )}

      {!loading && !error && hasSearched && !hasResults && (
        <p className="hint">{t('results.noResults', { query: result!.query })}</p>
      )}

      {!loading && hasResults && (
        <section className="results results-in" aria-label="Search results">
          {result!.recommendation && (
            <RecommendationBanner recommendation={result!.recommendation} />
          )}
          <h2 className="results-heading">
            {t('results.heading', { count: result!.offerCount, query: result!.query })}
          </h2>
          <OfferList offers={result!.offers} />
        </section>
      )}

      {!loading && !hasSearched && <FeatureHighlights />}
    </>
  )
}
