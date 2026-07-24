import { useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import type { AggregatedOffer } from '../api/types'
import { formatPrice } from '../format'
import { productArtFor } from '../productImage'
import { useTranslation } from '../i18n/useTranslation'
import { StoreLogo } from './StoreLogo'

type SortMode = 'price' | 'delivery'

interface OfferListProps {
  offers: AggregatedOffer[]
  query: string
  page: number
  totalPages: number
  onPageChange: (page: number) => void
}

/**
 * One continuous grid of offer cards, packed tightly (no wasted rows).
 * Grouping cards under a per-store section header used to leave a long,
 * mostly-empty row for every store with just 1-2 listings once many
 * different merchants started appearing — the store name/logo lives on
 * each card instead, same as before a store could return multiple
 * listings. Cards themselves still show only image/store/price/badge;
 * everything else (shipping, rating, stock, affiliate disclosure, the
 * actual "go to store" action) lives on the product detail page a card
 * links to, so a shopper isn't shown the same "here's the best deal"
 * story twice.
 */
export function OfferList({ offers, query, page, totalPages, onPageChange }: OfferListProps) {
  const { t } = useTranslation()
  const [sortMode, setSortMode] = useState<SortMode>('price')
  const [hideOutOfStock, setHideOutOfStock] = useState(false)

  const hasOutOfStock = offers.some((o) => !o.inStock)

  // Offers arrive rank-sorted from the API (isLowestPrice is fixed to the
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
        {sortedOffers.map((offer) => (
          <Link
            key={offer.listingId}
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

      {totalPages > 1 && (
        <div className="offer-list-pagination">
          <button
            type="button"
            className="offer-list-expand"
            disabled={page <= 1}
            onClick={() => onPageChange(page - 1)}
          >
            {t('results.prevPage')}
          </button>
          <span className="offer-list-page-status">{t('results.pageOf', { page, totalPages })}</span>
          <button
            type="button"
            className="offer-list-expand"
            disabled={page >= totalPages}
            onClick={() => onPageChange(page + 1)}
          >
            {t('results.nextPage')}
          </button>
        </div>
      )}
    </>
  )
}
