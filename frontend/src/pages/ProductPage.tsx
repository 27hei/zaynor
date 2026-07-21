import { useEffect, useRef, useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { createAlert, outboundUrl, saveProduct, searchProducts } from '../api/client'
import type { SearchResult } from '../api/types'
import { SearchBar } from '../components/SearchBar'
import { RecommendationBanner } from '../components/RecommendationBanner'
import { OfferList } from '../components/OfferList'
import { OfferListSkeleton } from '../components/OfferListSkeleton'
import { NoonFallbackLink } from '../components/NoonFallbackLink'
import { ProductSummary } from '../components/ProductSummary'
import { PriceHistorySection } from '../components/PriceHistorySection'
import { useTranslation } from '../i18n/useTranslation'
import { useAuth } from '../auth/useAuth'
import { usePageTitle } from '../hooks/usePageTitle'
import { useRecentSearches } from '../hooks/useRecentSearches'

/**
 * A product's own page: one URL per search, so a result is something a
 * visitor can bookmark, share, or come back to — not just transient state on
 * the homepage. Every search entry point (hero search, header search,
 * category/catalog cards) lands here; Home stays a pure discovery/landing
 * page (spec: "لكل منتج وضع صفحة منفصله").
 */
export function ProductPage() {
  const { t } = useTranslation()
  const { user } = useAuth()
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const initialQuery = searchParams.get('q') ?? ''

  const [query, setQuery] = useState(initialQuery)
  const [result, setResult] = useState<SearchResult | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [saved, setSaved] = useState(false)
  const [alertSet, setAlertSet] = useState(false)
  const [loadingLong, setLoadingLong] = useState(false)
  const [actionError, setActionError] = useState<string | null>(null)
  const activeRequest = useRef<AbortController | null>(null)
  const lastRunQuery = useRef<string | null>(null)
  const { recent, add: addRecent, clear: clearRecent } = useRecentSearches()

  usePageTitle(result ? t('seo.searchTitle', { query: result.query }) : query || undefined)

  async function handleSearch(searchQuery: string) {
    activeRequest.current?.abort()
    const controller = new AbortController()
    activeRequest.current = controller

    lastRunQuery.current = searchQuery
    setQuery(searchQuery)
    setLoading(true)
    setError(null)
    setSaved(false)
    setAlertSet(false)
    setActionError(null)
    navigate(`/product?q=${encodeURIComponent(searchQuery)}`, { replace: true })

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

  // Run the search from the ?q= this page was opened with, and re-run it if
  // the URL's query changes to a different product (e.g. the compact search
  // bar submits again, updating ?q= via replace above).
  useEffect(() => {
    const q = searchParams.get('q') ?? ''
    if (!q || q === lastRunQuery.current) return
    setQuery(q)
    handleSearch(q)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [searchParams])

  const hasResults = !!result && result.offers.length > 0
  // Noon has no live search feed (spec: no official API) — offer a direct
  // search fallback whenever it isn't already one of the real offers shown.
  const hasNoonOffer = !!result && result.offers.some((o) => o.storeName === 'Noon')

  const statusMessage = loading
    ? t('results.searching')
    : result
      ? hasResults
        ? t('results.heading', { count: result.offerCount, query: result.query })
        : t('results.noResults', { query: result.query })
      : ''

  return (
    <div className="product-page">
      <section className="hero hero-compact">
        <div className="hero-compact-search">
          <SearchBar
            value={query}
            onChange={setQuery}
            onSearch={handleSearch}
            disabled={loading}
            recentSearches={recent}
            onClearRecent={clearRecent}
          />
          <button type="button" className="search-reset" onClick={() => navigate('/')}>
            {t('product.backToSearch')}
          </button>
        </div>
      </section>

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

      {!loading && !error && result && !hasResults && (
        <>
          <p className="hint">{t('results.noResults', { query: result.query })}</p>
          <NoonFallbackLink query={result.query} />
        </>
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

          {!hasNoonOffer && <NoonFallbackLink query={result!.query} />}

          <p className="product-page-note">{t('product.pageNote')}</p>

          <PriceHistorySection key={result!.query} query={result!.query} />
        </section>
      )}
    </div>
  )
}
