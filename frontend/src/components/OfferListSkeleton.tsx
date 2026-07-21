/** Mirrors the real offer row's shape while a search is in flight. */
export function OfferListSkeleton() {
  return (
    <ul className="offer-list offer-list-skeleton" aria-hidden="true">
      {Array.from({ length: 4 }).map((_, i) => (
        <li className="offer offer-skeleton" key={i}>
          <div className="offer-main">
            <div className="skeleton-block skeleton-thumb" />
            <div className="skeleton-block skeleton-avatar" />
            <div className="offer-info">
              <span className="skeleton-line skeleton-line-store" />
              <span className="skeleton-line skeleton-line-shipping" />
            </div>
          </div>
          <div className="offer-side">
            <span className="skeleton-line skeleton-line-price" />
            <div className="skeleton-block skeleton-button" />
          </div>
        </li>
      ))}
    </ul>
  )
}
