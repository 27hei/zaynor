import { Logo } from './Logo'
import { Wordmark } from './Wordmark'

/** The full brand mark for expressive placements: icon, wordmark, tagline. */
export function BrandLockup() {
  return (
    <div className="brand-lockup">
      <Logo size={72} detailed />
      <Wordmark className="brand-lockup-wordmark" />
      <p className="brand-lockup-tagline">
        <span className="tagline-line" aria-hidden="true" />
        Smart Shopping Decisions
        <span className="tagline-line" aria-hidden="true" />
      </p>
    </div>
  )
}
