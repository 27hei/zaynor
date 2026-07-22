import { useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import type { AggregatedOffer } from '../api/types'
import { formatPrice } from '../format'
import { productArtFor } from '../productImage'
import { useTranslation } from '../i18n/useTranslation'
import { StoreLogo } from './StoreLogo'

const VISIBLE_BY_DEFAULT = 8
type SortMode = 'price' | 'delivery'

interface OfferListProps {
  offers: AggregatedOffer[]
  query: string
}

/**
 * A scannable grid of offer cards — image, store, price only. Everything
 * else (shipping, rating, stock, affiliate disclosure, the actual "go to
 * store" action) lives on the product detail page a card links to, so a
 * shopper isn't shown the same "here's the best deal" story twice: once as
 * a big recommendation banner and again as a highlighted list row.
 */
export function OfferList({ offers, query }: OfferListProps) {
  const { t } = useTranslation()
  const [expanded, setExpanded] = useState(false)
  const [sortMode, setSortMode] = useState<SortMode>('price')
  const [hideOutOfStock, setHideOutOfStock] = useState(false)

  const hasOutOfStock = offers.some((o) => !o.inStock)

  // Offers arrive price-sorted from the API (isLowestPrice is fixed to the
  // true cheapest regardless of display order, so re-sorting here for
  // display never mislabels the "best price" tag).
  const sortedOffers = useMemo(() => {
    const base = hideOutOfStock ? offers.filter((o) => o.inStock) : offers
    if (sortMode !== 'delivery') return base
    return [...base].sort((a, b) => {
      if (a.deliveryDays == null) return 1
      if (b.deliveryDays == null) return -1
      return a.deliveryDays - b.deliveryDays
    })
  }, [offers, sortMode, hideOutOfStock])

  const hasMore = sortedOffers.length > VISIBLE_BY_DEFAULT
  const visibleOffers = expanded ? sortedOffers : sortedOffers.slice(0, VISIBLE_BY_DEFAULT)
  const hiddenCount = sortedOffers.length - VISIBLE_BY_DEFAULT

  return (
    <>
      {offers.length > 1 && (
        <div className="offer-list-controls">
          <label className="offer-sort">
            <span>{t('results.sortLabel')}</span>
            <select value={sortMode} onChange={(e) => setSortMode(e.target.value as SortMode)}>
              <option value="price">{t('results.sortPrice')}</option>
              <option value="delivery">{t('results.sortDelivery')}</option>
            </select>
          </label>
          {hasOutOfStock && (
            <label className="offer-filter-toggle">
              <input
                type="checkbox"
                checked={hideOutOfStock}
                onChange={(e) => setHideOutOfStock(e.target.checked)}
              />
              {t('results.hideOutOfStock')}
            </label>
          )}
        </div>
      )}

      <div className="offer-grid">
        {visibleOffers.map((offer, index) => (
          <Link
            key={`${offer.storeName}-${index}`}
            to="/product/details"
            state={{ offer, query }}
            className={offer.isLowestPrice ? 'offer-card offer-card-lowest' : 'offer-card'}
          >
            {offer.isLowestPrice && <span className="offer-card-badge">{t('results.lowestPrice')}</span>}
            <img
              className={offer.inStock ? 'offer-card-image' : 'offer-card-image offer-card-image-oos'}
              src={offer.imageUrl ?? productArtFor(offer.productTitle)}
              alt=""
              aria-hidden="true"
              loading="lazy"
            />
            <span className="offer-card-store">
              <StoreLogo storeName={offer.storeName} />
              <span className="offer-card-store-name">{offer.storeName}</span>
            </span>
            <span className="offer-card-price">{formatPrice(offer.price, offer.currency)}</span>
          </Link>
        ))}
      </div>

      {hasMore && !expanded && (
        <button type="button" className="offer-list-expand" onClick={() => setExpanded(true)}>
          {t('results.showAll', { total: offers.length, more: hiddenCount })}
        </button>
      )}
    </>
  )
}
