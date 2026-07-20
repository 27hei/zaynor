import { useEffect, useRef, useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { createAlert, outboundUrl, saveProduct, searchProducts } from '../api/client'
import type { SearchResult } from '../api/types'
import { BrandMark } from '../components/BrandMark'
import { SearchBar } from '../components/SearchBar'
import { NeutralityBadge } from '../components/NeutralityBadge'
import { PopularSearches } from '../components/PopularSearches'
import { RecommendationBanner } from '../components/RecommendationBanner'
import { OfferList } from '../components/OfferList'
import { OfferListSkeleton } from '../components/OfferListSkeleton'
import { FeatureHighlights } from '../components/FeatureHighlights'
import { HomeCategories } from '../components/HomeCategories'
import { LivePricesTeaser } from '../components/LivePricesTeaser'
import { StoreLogo } from '../components/StoreLogo'
import { TRACKED_STORE_NAMES } from '../storeBrand'
import { ProductSummary } from '../components/ProductSummary'
import { PriceHistorySection } from '../components/PriceHistorySection'
import { useTranslation } from '../i18n/useTranslation'
import { useAuth } from '../auth/useAuth'
import { usePageTitle } from '../hooks/usePageTitle'
import { useRecentSearches } from '../hooks/useRecentSearches'

export function HomePage() {
  const { t } = useTranslation()
  const { user } = useAuth()
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const [query, setQuery] = useState('')
  const [result, setResult] = useState<SearchResult | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [saved, setSaved] = useState(false)
  const [alertSet, setAlertSet] = useState(false)
  const [loadingLong, setLoadingLong] = useState(false)
  const [actionError, setActionError] = useState<string | null>(null)
  const activeRequest = useRef<AbortController | null>(null)
  const consumedInitialQuery = useRef(false)
  const { recent, add: addRecent, clear: clearRecent } = useRecentSearches()

  usePageTitle(result ? t('seo.searchTitle', { query: result.query }) : undefined)

  async function handleSearch(searchQuery: string) {
    activeRequest.current?.abort()
    const controller = new AbortController()
    activeRequest.current = controller

    setQuery(searchQuery)
    setLoading(true)
    setError(null)
    setSaved(false)
    setAlertSet(false)
    setActionError(null)

    // Free-tier hosting cold-starts can take ~a minute; after a few seconds
    // of waiting, tell the user honestly instead of looking broken.
    setLoadingLong(false)
    const longTimer = window.setTimeout(() => setLoadingLong(true), 4000)

    try {
      const data = await searchProducts(searchQuery, controller.signal)
      setResult(data)
      addRecent(searchQuery)
    } catch (err) {
      if ((err as Error).name === 'AbortError') return
      setError((err as Error).message)
      setResult(null)
    } finally {
      window.clearTimeout(longTimer)
      if (activeRequest.current === controller) {
        setLoading(false)
        setLoadingLong(false)
      }
    }
  }

  // Save the searched product to the signed-in user's list (spec FR9).
  async function handleSave() {
    if (!user) {
      navigate('/login')
      return
    }
    setActionError(null)
    try {
      await saveProduct(result!.query)
      setSaved(true)
    } catch {
      setActionError(t('results.actionError'))
    }
  }

  // Subscribe to a price-drop alert, recording today's lowest price as the
  // baseline the future monitor compares against (spec FR8, Section 16/17).
  async function handleNotify() {
    if (!user) {
      navigate('/login')
      return
    }
    setActionError(null)
    const best = result!.offers.find((o) => o.isLowestPrice)
    try {
      await createAlert(result!.query, best?.price ?? null, best?.currency ?? null)
      setAlertSet(true)
    } catch {
      setActionError(t('results.actionError'))
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
            <BrandMark size={64} detailed />
          </div>
          <p className="hero-eyebrow">{t('hero.eyebrow')}</p>
          <h1 className="hero-title">{t('hero.title')}</h1>
          <p className="hero-subtitle">{t('hero.subtitle')}</p>

          <SearchBar value={query} onChange={setQuery} onSearch={handleSearch} disabled={loading} />
          <NeutralityBadge />
          <PopularSearches onSelect={handleSearch} />

          {recent.length > 0 && (
            <div className="popular-searches" aria-label={t('hero.recentLabel')}>
              <span className="popular-searches-label">{t('hero.recentLabel')}</span>
              {recent.map((recentQuery) => (
                <button
                  key={recentQuery}
                  type="button"
                  className="popular-chip"
                  onClick={() => handleSearch(recentQuery)}
                >
                  {recentQuery}
                </button>
              ))}
              <button type="button" className="recent-clear" onClick={clearRecent}>
                {t('hero.clearRecent')}
              </button>
            </div>
          )}

          <div className="hero-trust">
            <span className="hero-trust-label">{t('hero.trustLine')}</span>
            <div className="hero-trust-logos">
              {TRACKED_STORE_NAMES.map((store) => (
                <span key={store} className="hero-trust-logo" title={store}>
                  <StoreLogo storeName={store} />
                </span>
              ))}
            </div>
          </div>
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
          {loadingLong && <p className="hint hint-wake">{t('results.wakeHint')}</p>}
          <OfferListSkeleton />
        </section>
      )}

      {!loading && !error && hasSearched && !hasResults && (
        <p className="hint">{t('results.noResults', { query: result!.query })}</p>
      )}

      {!loading && hasResults && (
        <section className="results results-in" aria-label="Search results">
          {result!.isDemoData && (
            <p className="demo-banner" role="note">
              {t('results.demoData')}
            </p>
          )}

          <ProductSummary query={result!.query} offers={result!.offers} />

          {result!.recommendation && (
            <RecommendationBanner
              recommendation={result!.recommendation}
              bestUrl={(() => {
                const best = result!.offers.find((o) => o.isLowestPrice)
                return best ? outboundUrl(best.productUrl, best.storeName, result!.query) : undefined
              })()}
            />
          )}

          <div className="results-toolbar">
            <h2 className="results-heading">
              {t('results.heading', { count: result!.offerCount, query: result!.query })}
            </h2>
            <div className="results-actions">
              <button
                type="button"
                className="action-chip"
                onClick={handleNotify}
                disabled={alertSet}
              >
                {alertSet ? t('results.notifySet') : t('results.notify')}
              </button>
              <button type="button" className="action-chip" onClick={handleSave} disabled={saved}>
                {saved ? t('results.savedDone') : t('results.save')}
              </button>
            </div>
          </div>
          {actionError && (
            <p className="hint hint-error" role="alert">
              {actionError}
            </p>
          )}

          <OfferList offers={result!.offers} />

          <PriceHistorySection key={result!.query} query={result!.query} />
        </section>
      )}

      {!loading && !hasSearched && (
        <>
          <LivePricesTeaser onSelect={handleSearch} />
          <FeatureHighlights />
          <HomeCategories onSelect={handleSearch} />
        </>
      )}
    </>
  )
}
