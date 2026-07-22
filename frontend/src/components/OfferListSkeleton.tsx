/** Mirrors the real offer grid's shape while a search is in flight. */
export function OfferListSkeleton() {
  return (
    <div className="offer-grid" aria-hidden="true">
      {Array.from({ length: 4 }).map((_, i) => (
        <div className="offer-card offer-card-skeleton" key={i}>
          <div className="skeleton-block offer-card-image" />
          <div className="offer-card-store">
            <div className="skeleton-block skeleton-avatar" />
            <span className="skeleton-line skeleton-line-store" />
          </div>
          <span className="skeleton-line skeleton-line-price" />
        </div>
      ))}
    </div>
  )
}
