import { useState } from 'react'
import { STORE_BRAND } from './../storeBrand'

interface StoreLogoProps {
  storeName: string
}

/**
 * Deterministic color per store name (not random per render) — with the
 * open-scope Google Shopping source, most stores in a result list are ones
 * we've never seen before, so every unbranded avatar used to render as the
 * same flat gray circle. A stable per-name hue makes a 20-store list
 * scannable instead of a wall of identical initials.
 */
function colorForName(name: string): { bg: string; fg: string } {
  let hash = 0
  for (let i = 0; i < name.length; i++) {
    hash = name.charCodeAt(i) + ((hash << 5) - hash)
  }
  const hue = Math.abs(hash) % 360
  return { bg: `hsl(${hue}, 65%, 92%)`, fg: `hsl(${hue}, 45%, 32%)` }
}

/**
 * The real store brand mark (spec Section 22-adjacent: recognizable identity
 * builds trust faster than an abstract initial). Falls back to a colored-
 * initial avatar if the logo file is missing or fails to load, so an
 * unlisted/new store never renders broken.
 */
export function StoreLogo({ storeName }: StoreLogoProps) {
  const brand = STORE_BRAND[storeName]
  const [failed, setFailed] = useState(false)

  if (!brand || failed) {
    const { bg, fg } = colorForName(storeName)
    return (
      <span
        className="offer-avatar"
        aria-hidden="true"
        style={{ background: bg, color: fg, borderColor: bg }}
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
