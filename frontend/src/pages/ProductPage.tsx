import { useEffect, useRef, useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { createAlert, getSuggestions, outboundUrl, saveProduct, searchProducts } from '../api/client'
import type { SearchResult } from '../api/types'
import { CATEGORY_SEEDS } from '../categories'
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
import { useToast } from '../toast/useToast'

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
  const toast = useToast()
  const [searchParams] = useSearchParams()
  const initialQuery = searchParams.get('q') ?? ''

  const [query, setQuery] = useState(initialQuery)
  const [result, setResult] = useState<SearchResult | null>(null)
  const [loading, setLoading] = useState(false)
  const [saved, setSaved] = useState(false)
  const [alertSet, setAlertSet] = useState(false)
  const [loadingLong, setLoadingLong] = useState(false)
  const [altSuggestions, setAltSuggestions] = useState<string[]>([])
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
    setSaved(false)
    setAlertSet(false)
    setAltSuggestions([])
    navigate(`/product?q=${encodeURIComponent(searchQuery)}`, { replace: true })

    // Free-tier hosting cold-starts can take ~a minute; after a few seconds
    // of waiting, tell the user honestly instead of looking broken.
    setLoadingLong(false)
    const longTimer = window.setTimeout(() => setLoadingLong(true), 4000)

    try {
      const data = await searchProducts(searchQuery, controller.signal)
      setResult(data)
      addRecent(searchQuery)
      if (data.offers.length === 0) {
        loadAlternativeSuggestions(searchQuery)
      }
    } catch (err) {
      if ((err as Error).name === 'AbortError') return
      toast.push(t('results.error', { message: (err as Error).message }), 'error')
      setResult(null)
    } finally {
      window.clearTimeout(longTimer)
      if (activeRequest.current === controller) {
        setLoading(false)
        setLoadingLong(false)
      }
    }
  }

  // No results: never leave the visitor at a dead end. Try the first
  // significant word of their query against known product names; if that
  // also comes up empty, fall back to a fixed set of popular categories.
  async function loadAlternativeSuggestions(failedQuery: string) {
    const firstWord = failedQuery.trim().split(/\s+/)[0]
    try {
      const found = firstWord ? await getSuggestions(firstWord) : []
      setAltSuggestions(found.length > 0 ? found : CATEGORY_SEEDS.map((c) => c.seed))
    } catch {
      setAltSuggestions(CATEGORY_SEEDS.map((c) => c.seed))
    }
  }

  // Share this product's own URL — native share sheet where available
  // (mainly mobile), otherwise copy the link and confirm via toast.
  async function handleShare() {
    const shareUrl = window.location.href
    if (navigator.share) {
      try {
        await navigator.share({ title: result?.query, url: shareUrl })
      } catch {
        // User cancelled the share sheet — not an error.
      }
      return
    }
    try {
      await navigator.clipboard.writeText(shareUrl)
      toast.push(t('results.linkCopied'), 'success')
    } catch {
      toast.push(t('results.actionError'), 'error')
    }
  }

  // Save the searched product to the signed-in user's list (spec FR9).
  async function handleSave() {
    if (!user) {
      navigate('/login')
      return
    }
    try {
      await saveProduct(result!.query)
      setSaved(true)
      toast.push(t('results.savedDone'), 'success')
    } catch {
      toast.push(t('results.actionError'), 'error')
    }
  }

  // Subscribe to a price-drop alert, recording today's lowest price as the
  // baseline the future monitor compares against (spec FR8, Section 16/17).
  async function handleNotify() {
    if (!user) {
      navigate('/login')
      return
    }
    const best = result!.offers.find((o) => o.isLowestPrice)
    try {
      await createAlert(result!.query, best?.price ?? null, best?.currency ?? null)
      setAlertSet(true)
      toast.push(t('results.notifySet'), 'success')
    } catch {
      toast.push(t('results.actionError'), 'error')
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

      {!loading && result?.correctedQuery && (
        <p className="corrected-query-note">
          {t('results.correctedQuery', { corrected: result.correctedQuery })}
        </p>
      )}

      {loading && (
        <section className="results" aria-label={t('results.searching')} aria-busy="true">
          <p className="hint">{t('results.searching')}</p>
          {loadingLong && <p className="hint hint-wake">{t('results.wakeHint')}</p>}
          <OfferListSkeleton />
        </section>
      )}

      {!loading && result && !hasResults && (
        <>
          <p className="hint">{t('results.noResults', { query: result.query })}</p>
          {altSuggestions.length > 0 && (
            <div className="no-results-suggestions">
              <span className="no-results-suggestions-label">{t('results.tryInstead')}</span>
              <div className="no-results-suggestions-row">
                {altSuggestions.map((s) => (
                  <button key={s} type="button" className="popular-chip" onClick={() => handleSearch(s)}>
                    {s}
                  </button>
                ))}
              </div>
            </div>
          )}
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
                return best ? outboundUrl(best.productUrl, best.storeName, result!.query, best.signature) : undefined
              })()}
            />
          )}

          <div className="results-toolbar">
            <h2 className="results-heading">
              {t('results.heading', { count: result!.offerCount, query: result!.query })}
            </h2>
            <div className="results-actions">
              <button type="button" className="action-chip" onClick={handleShare}>
                {t('results.share')}
              </button>
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

          <OfferList offers={result!.offers} />

          {!hasNoonOffer && <NoonFallbackLink query={result!.query} />}

          <p className="product-page-note">{t('product.pageNote')}</p>

          <PriceHistorySection key={result!.query} query={result!.query} />
        </section>
      )}
    </div>
  )
}
