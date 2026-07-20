import { useEffect, useState } from 'react'
import { getCatalog, type CatalogSummary } from '../api/client'
import { formatPrice } from '../format'
import { productArtFor } from '../productImage'
import { useTranslation } from '../i18n/useTranslation'

const MAX_SHOWN = 4

interface LivePricesTeaserProps {
  onSelect: (query: string) => void
}

/**
 * Proof, not promises: real products with real current prices, right under
 * the search box — so a first-time visitor sees Zaynor working before
 * reading a single word about what it does (spec Section 4: the value is the
 * comparison itself). Clicking a card runs the same real search a manual
 * query would.
 */
export function LivePricesTeaser({ onSelect }: LivePricesTeaserProps) {
  const { t } = useTranslation()
  const [items, setItems] = useState<CatalogSummary[] | null>(null)

  useEffect(() => {
    let cancelled = false
    getCatalog()
      .then((all) => {
        if (!cancelled) setItems(all.slice(0, MAX_SHOWN))
      })
      .catch(() => {
        if (!cancelled) setItems([])
      })
    return () => {
      cancelled = true
    }
  }, [])

  if (!items || items.length === 0) {
    return null
  }

  return (
    <section className="live-teaser" aria-label={t('teaser.title')}>
      <h2 className="live-teaser-title">{t('teaser.title')}</h2>
      <div className="live-teaser-grid">
        {items.map((item) => (
          <button
            key={item.name}
            type="button"
            className="live-teaser-card"
            onClick={() => onSelect(item.name)}
          >
            <img
              className="live-teaser-art"
              src={item.image ?? productArtFor(item.name)}
              alt=""
              aria-hidden="true"
              loading="lazy"
            />
            <span className="live-teaser-name">{item.name}</span>
            <span className="live-teaser-price">
              {t('catalog.from', { price: formatPrice(item.lowestPrice, item.currency) })}
            </span>
          </button>
        ))}
      </div>
    </section>
  )
}
