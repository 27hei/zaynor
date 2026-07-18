import { useState } from 'react'
import type { AggregatedOffer } from '../api/types'
import { formatPrice } from '../format'

const VISIBLE_BY_DEFAULT = 3

interface OfferListProps {
  offers: AggregatedOffer[]
}

export function OfferList({ offers }: OfferListProps) {
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
              {offer.isLowestPrice && <span className="offer-tag">Lowest price</span>}
              {!offer.inStock && <span className="offer-oos">Out of stock</span>}
            </div>
            <div className="offer-side">
              <span className="offer-price">{formatPrice(offer.price, offer.currency)}</span>
              <a
                className="offer-link"
                href={offer.productUrl}
                target="_blank"
                rel="noopener noreferrer sponsored"
              >
                Go to store
              </a>
            </div>
          </li>
        ))}
      </ul>

      {hasMore && !expanded && (
        <button type="button" className="offer-list-expand" onClick={() => setExpanded(true)}>
          Show all {offers.length} offers ({hiddenCount} more)
        </button>
      )}
    </>
  )
}
