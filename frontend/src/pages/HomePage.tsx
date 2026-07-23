import { useNavigate } from 'react-router-dom'
import { SearchBar } from '../components/SearchBar'
import { NeutralityBadge } from '../components/NeutralityBadge'
import { HomeCategories } from '../components/HomeCategories'
import { NoonHomeBanner } from '../components/NoonHomeBanner'
import { ZaynorTestimonials } from '../components/ZaynorTestimonials'
import { PhoneMockup } from '../components/PhoneMockup'
import {
  SavingsIcon,
  IntelligenceIcon,
  RefreshIcon,
  DiscoveryIcon,
  TrustIcon,
  PlayGlyphIcon,
  AppleGlyphIcon,
} from '../components/icons'
import { useTranslation } from '../i18n/useTranslation'
import { usePageTitle } from '../hooks/usePageTitle'
import { useRecentSearches } from '../hooks/useRecentSearches'
import { useState } from 'react'

/**
 * A pure landing/discovery page: hero, search box, and browse-by-category
 * shortcuts. Every search (typed here, or a category/live-price card) opens
 * that product's own page at /product?q=... — Home never renders results
 * itself (spec: "لكل منتج وضع صفحه منفصله").
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
      <section className="hero hero-split">
        <div className="hero-split-inner">
          <div className="hero-content">
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

            <div className="hero-features-row">
              <span className="hero-feature">
                <SavingsIcon />
                {t('feature.saveTimeMoney')}
              </span>
              <span className="hero-feature">
                <IntelligenceIcon />
                {t('feature.smartRecommendations')}
              </span>
              <span className="hero-feature">
                <RefreshIcon />
                {t('feature.livePrices')}
              </span>
            </div>

            <div className="hero-app-download">
              <div className="hero-app-badges">
                <span className="app-badge" title={t('app.comingSoon')}>
                  <PlayGlyphIcon className="app-badge-icon" />
                  <span className="app-badge-text">
                    <small>GET IT ON</small>
                    Google Play
                  </span>
                </span>
                <span className="app-badge" title={t('app.comingSoon')}>
                  <AppleGlyphIcon className="app-badge-icon" />
                  <span className="app-badge-text">
                    <small>Download on the</small>
                    App Store
                  </span>
                </span>
              </div>
              <p className="hero-app-text">
                <strong>{t('app.downloadTitle')}</strong> {t('app.downloadSubtitle')}
              </p>
            </div>
          </div>

          <PhoneMockup />
        </div>
      </section>

      <div className="hero-stats-bar">
        <div className="hero-stat">
          <SavingsIcon />
          <span>{t('stats.saveUpTo')}</span>
        </div>
        <div className="hero-stat">
          <DiscoveryIcon />
          <span>{t('stats.searchCount')}</span>
        </div>
        <div className="hero-stat">
          <TrustIcon />
          <span>{t('stats.storeCount')}</span>
        </div>
        <div className="hero-stat">
          <RefreshIcon />
          <span>{t('stats.productCount')}</span>
        </div>
      </div>

      <HomeCategories onSelect={goToProduct} />
      <NoonHomeBanner />
      <ZaynorTestimonials />
    </>
  )
}
