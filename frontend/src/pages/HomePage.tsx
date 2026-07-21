import { useNavigate } from 'react-router-dom'
import { BrandMark } from '../components/BrandMark'
import { SearchBar } from '../components/SearchBar'
import { NeutralityBadge } from '../components/NeutralityBadge'
import { HomeCategories } from '../components/HomeCategories'
import { LivePricesTeaser } from '../components/LivePricesTeaser'
import { StoreLogo } from '../components/StoreLogo'
import { TRACKED_STORE_NAMES } from '../storeBrand'
import { useTranslation } from '../i18n/useTranslation'
import { usePageTitle } from '../hooks/usePageTitle'
import { useRecentSearches } from '../hooks/useRecentSearches'
import { useState } from 'react'

/**
 * A pure landing/discovery page: hero, search box, and browse-by-category
 * shortcuts. Every search (typed here, or a category/live-price card) opens
 * that product's own page at /product?q=... — Home never renders results
 * itself (spec: "لكل منتج وضع صفحة منفصله").
 */
export function HomePage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const [query, setQuery] = useState('')
  const { recent, clear: clearRecent } = useRecentSearches()

  usePageTitle(undefined)

  function goToProduct(searchQuery: string) {
    navigate(`/product?q=${encodeURIComponent(searchQuery)}`)
  }

  return (
    <>
      <section className="hero">
        <div className="hero-mark">
          <BrandMark size={64} detailed />
        </div>
        <p className="hero-eyebrow">{t('hero.eyebrow')}</p>
        <h1 className="hero-title">{t('hero.title')}</h1>
        <p className="hero-subtitle">{t('hero.subtitle')}</p>

        <SearchBar
          value={query}
          onChange={setQuery}
          onSearch={goToProduct}
          recentSearches={recent}
          onClearRecent={clearRecent}
        />
        <NeutralityBadge />

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

      <LivePricesTeaser onSelect={goToProduct} />
      <HomeCategories onSelect={goToProduct} />
    </>
  )
}
