import type { AggregatedOffer } from '../api/types'
import { formatPrice } from '../format'

interface OfferListProps {
  offers: AggregatedOffer[]
}

export function OfferList({ offers }: OfferListProps) {
  return (
    <ul className="offer-list">
      {offers.map((offer, index) => (
        <li
          key={`${offer.storeName}-${index}`}
          className={offer.isLowestPrice ? 'offer offer-lowest' : 'offer'}
        >
          <div className="offer-main">
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
  )
}
