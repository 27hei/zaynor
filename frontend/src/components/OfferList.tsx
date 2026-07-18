import { useState } from 'react'
import type { AggregatedOffer } from '../api/types'
import { formatPrice } from '../format'
import { useTranslation } from '../i18n/useTranslation'

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

  return (
    <>
      <ul className="offer-list">
        {visibleOffers.map((offer, index) => (
          <li
            key={`${offer.storeName}-${index}`}
            className={offer.isLowestPrice ? 'offer offer-lowest' : 'offer'}
          >
            <div className="offer-main">
              <span className="offer-avatar" aria-hidden="true">
                {offer.storeName.charAt(0).toUpperCase()}
              </span>
              <span className="offer-store">{offer.storeName}</span>
              {offer.isLowestPrice && <span className="offer-tag">{t('results.lowestPrice')}</span>}
              {!offer.inStock && <span className="offer-oos">{t('results.outOfStock')}</span>}
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
        ))}
      </ul>

      {hasMore && !expanded && (
        <button type="button" className="offer-list-expand" onClick={() => setExpanded(true)}>
          {t('results.showAll', { total: offers.length, more: hiddenCount })}
        </button>
      )}
    </>
  )
}
