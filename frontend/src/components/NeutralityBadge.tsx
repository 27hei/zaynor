import { TrustIcon } from './icons'

/**
 * A loud, upfront neutrality signal (competitive analysis Section 3.2 / 6.D):
 * most comparison sites quietly favor higher-commission stores. Zaynor's
 * spec explicitly commits to ranking by price alone (Section 6, "Recommendation
 * neutrality"), so this promise is real, not marketing — safe to state plainly.
 */
export function NeutralityBadge() {
  return (
    <p className="neutrality-badge">
      <TrustIcon className="neutrality-icon" />
      100% impartial — every store ranked by price alone, never by commission
    </p>
  )
}
