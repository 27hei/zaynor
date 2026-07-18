import { useRef, useState } from 'react'
import './App.css'
import { searchProducts } from './api/client'
import type { SearchResult } from './api/types'
import { Header } from './components/Header'
import { Footer } from './components/Footer'
import { SearchBar } from './components/SearchBar'
import { RecommendationBanner } from './components/RecommendationBanner'
import { OfferList } from './components/OfferList'
import { OfferListSkeleton } from './components/OfferListSkeleton'
import { FeatureHighlights } from './components/FeatureHighlights'

const TRACKED_STORES = ['Amazon.sa', 'Noon', 'Jarir', 'Extra', 'AliExpress']

function App() {
  const [result, setResult] = useState<SearchResult | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const activeRequest = useRef<AbortController | null>(null)

  async function handleSearch(query: string) {
    // Cancel any in-flight request so stale results can't overwrite newer ones.
    activeRequest.current?.abort()
    const controller = new AbortController()
    activeRequest.current = controller

    setLoading(true)
    setError(null)

    try {
      const data = await searchProducts(query, controller.signal)
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

  const hasSearched = result !== null
  const hasResults = !!result && result.offers.length > 0

  return (
    <div className="page">
      <Header />

      <main className="content">
        <section className="hero">
          <h1 className="hero-title">Compare prices. Buy with confidence.</h1>
          <p className="hero-subtitle">
            Search once — Zaynor checks every store, finds the lowest price, and tells you where
            to buy.
          </p>

          <SearchBar onSearch={handleSearch} disabled={loading} />

          <p className="hero-trust">
            Comparing offers across{' '}
            {TRACKED_STORES.map((store, i) => (
              <span key={store}>
                <span className="hero-trust-store">{store}</span>
                {i < TRACKED_STORES.length - 1 && <span> · </span>}
              </span>
            ))}
          </p>
        </section>

        {error && (
          <p className="hint hint-error" role="alert">
            Search failed: {error}
          </p>
        )}

        {loading && (
          <section className="results" aria-label="Searching" aria-busy="true">
            <p className="hint">Searching stores…</p>
            <OfferListSkeleton />
          </section>
        )}

        {!loading && !error && hasSearched && !hasResults && (
          <p className="hint">No offers found for "{result!.query}".</p>
        )}

        {!loading && hasResults && (
          <section className="results" aria-label="Search results">
            {result!.recommendation && (
              <RecommendationBanner recommendation={result!.recommendation} />
            )}
            <h2 className="results-heading">
              {result!.offerCount} offers for "{result!.query}"
            </h2>
            <OfferList offers={result!.offers} />
          </section>
        )}

        {!loading && !hasSearched && <FeatureHighlights />}
      </main>

      <Footer />
    </div>
  )
}

export default App
