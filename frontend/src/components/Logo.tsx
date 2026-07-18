import { useId } from 'react'

interface LogoProps {
  size?: number
  /** Full multi-icon ribbon mark for larger, expressive placements (hero). */
  detailed?: boolean
}

/**
 * The Zaynor Z-mark (spec Section 22): a thick rounded "Z" ribbon carrying the
 * project's five founding icons — price tag, checkmark, search, cart, and
 * rising analytics bar — read in order along the stroke.
 *
 * At small sizes (`detailed=false`, the default) those pictograms are too
 * fine to read, so the flat single-color glyph is used instead, per the
 * spec's own note to keep a flat version alongside the full mark for small
 * sizes such as the header and favicon.
 */
export function Logo({ size = 36, detailed = false }: LogoProps) {
  const gradientId = useId()

  if (!detailed) {
    return (
      <svg width={size} height={size} viewBox="0 0 64 64" role="img" aria-label="Zaynor logo">
        <defs>
          <linearGradient id={gradientId} x1="0" y1="0" x2="64" y2="64" gradientUnits="userSpaceOnUse">
            <stop offset="0" stopColor="#1D9E75" />
            <stop offset="1" stopColor="#0F6E56" />
          </linearGradient>
        </defs>
        <rect width="64" height="64" rx="16" fill={`url(#${gradientId})`} />
        <path d="M20 20h24v6.2L28.4 43.8H44V50H19.4v-6.2L35 26.2H20z" fill="#E1F5EE" />
      </svg>
    )
  }

  const ivory = '#EAF9F2'

  return (
    <svg
      width={size}
      height={(size * 100) / 92}
      viewBox="0 0 92 100"
      role="img"
      aria-label="Zaynor logo"
    >
      <defs>
        <linearGradient id={gradientId} x1="4" y1="10" x2="88" y2="92" gradientUnits="userSpaceOnUse">
          <stop offset="0" stopColor="#22B589" />
          <stop offset="1" stopColor="#0B5A45" />
        </linearGradient>
      </defs>

      {/* The Z ribbon: top bar (tag, check) / diagonal (search) / bottom bar (cart, chart). */}
      <path
        d="M12 18 H72 L20 82 H80"
        fill="none"
        stroke={`url(#${gradientId})`}
        strokeWidth="20"
        strokeLinecap="round"
        strokeLinejoin="round"
      />

      {/* Price tag hole */}
      <circle cx="21" cy="18" r="3.2" fill={ivory} />

      {/* Checkmark */}
      <path
        d="M52 18.5 L57 23.5 L67 12"
        fill="none"
        stroke={ivory}
        strokeWidth="4.2"
        strokeLinecap="round"
        strokeLinejoin="round"
      />

      {/* Magnifying glass */}
      <circle cx="44.5" cy="49.5" r="7.5" fill="none" stroke={ivory} strokeWidth="3.8" />
      <line x1="49.8" y1="54.8" x2="55" y2="60" stroke={ivory} strokeWidth="4.2" strokeLinecap="round" />

      {/* Cart */}
      <path
        d="M25.5 76.5 h3.5 l2.6 11.5 h12.5"
        fill="none"
        stroke={ivory}
        strokeWidth="3"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
      <circle cx="34.5" cy="91.2" r="2.1" fill={ivory} />
      <circle cx="43" cy="91.2" r="2.1" fill={ivory} />

      {/* Rising bar chart */}
      <rect x="61.5" y="84.5" width="4.4" height="7.5" rx="1.2" fill={ivory} />
      <rect x="68.3" y="79.5" width="4.4" height="12.5" rx="1.2" fill={ivory} />
      <rect x="75.1" y="73" width="4.4" height="19" rx="1.2" fill={ivory} />
    </svg>
  )
}
