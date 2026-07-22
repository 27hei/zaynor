import { useMemo, useState } from 'react'
import type { AggregatedOffer } from '../api/types'
import { formatPrice } from '../format'
import { useTranslation } from '../i18n/useTranslation'
import { StoreLogo } from './StoreLogo'
import { StoreRating } from './StoreRating'
import { outboundUrl } from '../api/client'

const VISIBLE_BY_DEFAULT = 3
type SortMode = 'price' | 'delivery'

interface OfferListProps {
  offers: AggregatedOffer[]
}

export function OfferList({ offers }: OfferListProps) {
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

  function shippingLabel(offer: AggregatedOffer): string | null {
    const parts: string[] = []
    if (offer.freeShipping) {
      parts.push(t('offer.freeShipping'))
    }
    if (offer.deliveryDays != null) {
      parts.push(
        offer.deliveryDays <= 1
          ? t('offer.deliveryNextDay')
          : t('offer.delivery', { days: offer.deliveryDays }),
      )
    }
    return parts.length > 0 ? parts.join(' · ') : null
  }

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

      {sortedOffers.length > 0 && (
        <div className="offer-table-head" aria-hidden="true">
          <span>{t('results.colStore')}</span>
          <span>{t('results.colPrice')}</span>
        </div>
      )}

      <ul className="offer-list">
        {visibleOffers.map((offer, index) => {
          const shipping = shippingLabel(offer)

          return (
            <li
              key={`${offer.storeName}-${index}`}
              className={offer.isLowestPrice ? 'offer offer-lowest' : 'offer'}
            >
              <div className="offer-main">
                {offer.imageUrl && (
                  <img className="offer-thumb" src={offer.imageUrl} alt="" aria-hidden="true" loading="lazy" />
                )}
                <StoreLogo storeName={offer.storeName} />
                <div className="offer-info">
                  <span className="offer-store-row">
                    <span className="offer-store">{offer.storeName}</span>
                    {offer.isLowestPrice && (
                      <span className="offer-tag">{t('results.lowestPrice')}</span>
                    )}
                    {!offer.inStock && <span className="offer-oos">{t('results.outOfStock')}</span>}
                    {offer.rating != null && (
                      <StoreRating rating={offer.rating} ratingCount={offer.ratingCount} />
                    )}
                    {offer.hasAffiliateLink && (
                      <span className="offer-affiliate-badge" title={t('offer.affiliateHint')}>
                        <svg width="11" height="11" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
                          <path d="M12 21s-7.5-4.6-10-9.2C.5 8.4 2.3 5 5.8 5c1.9 0 3.5 1 4.5 2.6C11.3 6 12.9 5 14.8 5c3.5 0 5.3 3.4 3.8 6.8C19.5 16.4 12 21 12 21z" />
                        </svg>
                        {t('offer.affiliateBadge')}
                      </span>
                    )}
                  </span>
                  {shipping && <span className="offer-shipping">{shipping}</span>}
                </div>
              </div>
              <div className="offer-side">
                <span className="offer-price">{formatPrice(offer.price, offer.currency)}</span>
                <span className="offer-link-wrap">
                  <a
                    className="offer-link"
                    href={outboundUrl(offer.productUrl, offer.storeName, offer.productTitle, offer.signature)}
                    target="_blank"
                    rel="noopener noreferrer sponsored"
                  >
                    {t('results.goToStore')}
                  </a>
                </span>
              </div>
            </li>
          )
        })}
      </ul>

      {hasMore && !expanded && (
        <button type="button" className="offer-list-expand" onClick={() => setExpanded(true)}>
          {t('results.showAll', { total: offers.length, more: hiddenCount })}
        </button>
      )}
    </>
  )
}
