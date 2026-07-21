import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { getCatalog, type CatalogSummary } from '../api/client'
import { formatPrice } from '../format'
import { productArtFor } from '../productImage'
import { useTranslation } from '../i18n/useTranslation'
import { usePageTitle } from '../hooks/usePageTitle'
import { CATEGORY_SEEDS, type CategoryKey } from '../categories'
import {
  AnalysisIcon,
  DiscoveryIcon,
  IntelligenceIcon,
  SavingsIcon,
  AlertsIcon,
  TrustIcon,
} from '../components/icons'

const CATEGORY_ICONS: Record<CategoryKey, typeof DiscoveryIcon> = {
  electronics: DiscoveryIcon,
  gaming: IntelligenceIcon,
  phones: SavingsIcon,
  computers: AnalysisIcon,
  tv: TrustIcon,
  appliances: AlertsIcon,
}

/**
 * Real category browsing (FR10): covered products from the curated catalog,
 * grouped by category with genuine lowest prices. Categories without coverage
 * yet fall back to seeded searches so nothing is a dead end.
 */
export function CategoriesPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const [catalog, setCatalog] = useState<CatalogSummary[]>([])

  usePageTitle(t('nav.categories'))

  useEffect(() => {
    getCatalog().then(setCatalog).catch(() => setCatalog([]))
  }, [])

  const covered = new Set(catalog.map((p) => p.category))

  return (
    <section className="page-article">
      <h1 className="page-title">{t('categories.title')}</h1>
      <p className="page-subtitle">{t('categories.subtitle')}</p>

      {/* Real covered products, grouped by category */}
      {[...covered].map((category) => (
        <div key={category} className="catalog-group">
          <h2 className="catalog-group-title">{t(`category.${category}`)}</h2>
          <ul className="catalog-list">
            {catalog
              .filter((p) => p.category === category)
              .map((p) => (
                <li key={p.name}>
                  <button
                    type="button"
                    className="catalog-item"
                    onClick={() => navigate(`/product?q=${encodeURIComponent(p.name)}`)}
                  >
                    <img
                      className="catalog-item-art"
                      src={p.image ?? productArtFor(p.name)}
                      alt=""
                      aria-hidden="true"
                      loading="lazy"
                    />
                    <span className="catalog-item-info">
                      <span className="catalog-item-name">{p.name}</span>
                      <span className="catalog-item-price">
                        {t('catalog.from', { price: formatPrice(p.lowestPrice, p.currency) })}
                      </span>
                    </span>
                  </button>
                </li>
              ))}
          </ul>
        </div>
      ))}

      {/* Uncovered categories still seed a search */}
      <div className="category-grid">
        {CATEGORY_SEEDS.filter(({ key }) => !covered.has(key)).map(({ key, seed }) => {
          const Icon = CATEGORY_ICONS[key]
          return (
            <button
              key={key}
              type="button"
              className="category-card"
              onClick={() => navigate(`/product?q=${encodeURIComponent(seed)}`)}
            >
              <span className="category-icon">
                <Icon />
              </span>
              <span className="category-name">{t(`category.${key}`)}</span>
              <span className="category-search-hint">{t('categories.searchHint')}</span>
            </button>
          )
        })}
      </div>
    </section>
  )
}
