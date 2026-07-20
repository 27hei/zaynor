import { useState } from 'react'
import { STORE_BRAND } from './../storeBrand'

interface StoreLogoProps {
  storeName: string
}

/**
 * The real store brand mark (spec Section 22-adjacent: recognizable identity
 * builds trust faster than an abstract initial). Falls back to the existing
 * colored-initial avatar if the logo file is missing or fails to load, so an
 * unlisted/new store never renders broken.
 */
export function StoreLogo({ storeName }: StoreLogoProps) {
  const brand = STORE_BRAND[storeName]
  const [failed, setFailed] = useState(false)

  if (!brand || failed) {
    return (
      <span
        className="offer-avatar"
        aria-hidden="true"
        style={brand ? { background: brand.bg, color: brand.fg, borderColor: brand.bg } : undefined}
      >
        {storeName.charAt(0).toUpperCase()}
      </span>
    )
  }

  return (
    <span className="offer-avatar offer-avatar-logo" aria-hidden="true">
      <img src={brand.logo} alt="" onError={() => setFailed(true)} />
    </span>
  )
}
