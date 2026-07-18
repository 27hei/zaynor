import { useState } from 'react'
import type { AggregatedOffer } from '../api/types'
import { formatPrice } from '../format'
import { useTranslation } from '../i18n/useTranslation'
import { STORE_BRAND } from '../storeBrand'

const VISIBLE_BY_DEFAULT = 3

interface OfferListProps {
  offers: AggregatedOffer[]
}

export function OfferList({ offers }: OfferListProps) {
  const { t } = useTranslation()
  const [expanded, setExpanded] = useState(false)

  const hasMore = offers.length > VISIBLE_BY_DEFAULT
  const visibleOffers = expanded ? offers : offers.slice(0, VISIBLE_BY_DEFAULT)
  const hiddenCount = offers.length - VISIBLE_BY_DEFAULT

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
      <ul className="offer-list">
        {visibleOffers.map((offer, index) => {
          const brand = STORE_BRAND[offer.storeName]
          const shipping = shippingLabel(offer)

          return (
            <li
              key={`${offer.storeName}-${index}`}
              className={offer.isLowestPrice ? 'offer offer-lowest' : 'offer'}
            >
              <div className="offer-main">
                <span
                  className="offer-avatar"
                  aria-hidden="true"
                  style={brand ? { background: brand.bg, color: brand.fg, borderColor: brand.bg } : undefined}
                >
                  {offer.storeName.charAt(0).toUpperCase()}
                </span>
                <div className="offer-info">
                  <span className="offer-store-row">
                    <span className="offer-store">{offer.storeName}</span>
                    {offer.isLowestPrice && (
                      <span className="offer-tag">{t('results.lowestPrice')}</span>
                    )}
                    {!offer.inStock && <span className="offer-oos">{t('results.outOfStock')}</span>}
                  </span>
                  {shipping && <span className="offer-shipping">{shipping}</span>}
                </div>
              </div>
              <div className="offer-side">
                <span className="offer-price">{formatPrice(offer.price, offer.currency)}</span>
                <a
                  className="offer-link"
                  href={offer.productUrl}
                  target="_blank"
                  rel="noopener noreferrer sponsored"
                >
                  {t('results.goToStore')}
                </a>
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
