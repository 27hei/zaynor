import { useRef, useState } from 'react'
import './App.css'
import { searchProducts } from './api/client'
import type { SearchResult } from './api/types'
import { SearchBar } from './components/SearchBar'
import { RecommendationBanner } from './components/RecommendationBanner'
import { OfferList } from './components/OfferList'

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

  const hasResults = result && result.offers.length > 0

  return (
    <div className="page">
      <header className="masthead">
        <h1 className="brand">ZAYNOR</h1>
        <p className="tagline">Smart Shopping Decisions</p>
      </header>

      <main className="content">
        <SearchBar onSearch={handleSearch} disabled={loading} />

        {loading && <p className="hint">Searching stores…</p>}
        {error && <p className="hint hint-error">Search failed: {error}</p>}

        {!loading && !error && result && !hasResults && (
          <p className="hint">No offers found for “{result.query}”.</p>
        )}

        {!loading && hasResults && (
          <section className="results" aria-label="Search results">
            {result.recommendation && (
              <RecommendationBanner recommendation={result.recommendation} />
            )}
            <h2 className="results-heading">
              {result.offerCount} offers for “{result.query}”
            </h2>
            <OfferList offers={result.offers} />
          </section>
        )}
      </main>
    </div>
  )
}

export default App
