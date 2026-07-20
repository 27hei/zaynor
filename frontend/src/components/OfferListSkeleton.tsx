export function OfferListSkeleton() {
  return (
    <ul className="offer-list offer-list-skeleton" aria-hidden="true">
      {Array.from({ length: 4 }).map((_, i) => (
        <li className="offer offer-skeleton" key={i}>
          <div className="skeleton-line skeleton-line-wide" />
          <div className="skeleton-line skeleton-line-narrow" />
        </li>
      ))}
    </ul>
  )
}
