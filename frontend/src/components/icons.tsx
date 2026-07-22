// Small stroke icons for the feature highlights, matching the brand's icon
// set (ثقة Trust, ذكاء Intelligence, توفير Savings, اكتشاف Discovery,
// تحليل Analysis, تنبيهات Alerts). Inline SVG so there's no icon-library
// dependency for a handful of glyphs.

import type { ReactElement } from 'react'

type IconProps = { className?: string }
export type IconComponent = (props: IconProps) => ReactElement

const common = {
  width: 22,
  height: 22,
  viewBox: '0 0 24 24',
  fill: 'none',
  stroke: 'currentColor',
  strokeWidth: 1.8,
  strokeLinecap: 'round' as const,
  strokeLinejoin: 'round' as const,
}

export function TrustIcon({ className }: IconProps) {
  return (
    <svg {...common} className={className}>
      <path d="M12 3l7 3v6c0 4.5-3 7.5-7 9-4-1.5-7-4.5-7-9V6l7-3z" />
      <path d="M9 12l2 2 4-4" />
    </svg>
  )
}

/** A spark/AI glyph for Intelligence — a four-point star burst. */
export function IntelligenceIcon({ className }: IconProps) {
  return (
    <svg {...common} className={className}>
      <path d="M12 3c.6 3.4 2.6 5.4 6 6-3.4.6-5.4 2.6-6 6-.6-3.4-2.6-5.4-6-6 3.4-.6 5.4-2.6 6-6z" />
      <path d="M19 16.5c.25 1.4 1 2.15 2.4 2.4-1.4.25-2.15 1-2.4 2.4-.25-1.4-1-2.15-2.4-2.4 1.4-.25 2.15-1 2.4-2.4z" />
    </svg>
  )
}

/** A price tag for Savings. */
export function SavingsIcon({ className }: IconProps) {
  return (
    <svg {...common} className={className}>
      <path d="M20 12 12.6 4.6a2 2 0 0 0-1.4-.6H5a1 1 0 0 0-1 1v6.2a2 2 0 0 0 .6 1.4L12 20l8-8z" />
      <circle cx="9" cy="9" r="1.4" />
    </svg>
  )
}

/** A magnifying glass with a spark accent for Discovery. */
export function DiscoveryIcon({ className }: IconProps) {
  return (
    <svg {...common} className={className}>
      <circle cx="10.5" cy="10.5" r="6.5" />
      <path d="m19.5 19.5-4-4" />
      <path d="M18.5 4.5c.15 1 .55 1.4 1.5 1.5-.95.1-1.35.5-1.5 1.5-.15-1-.55-1.4-1.5-1.5.95-.1 1.35-.5 1.5-1.5z" />
    </svg>
  )
}

/** Rising bars with an uptrend line for Analysis. */
export function AnalysisIcon({ className }: IconProps) {
  return (
    <svg {...common} className={className}>
      <path d="M4 20V13M12 20V9M20 20v-6" />
      <path d="M4 9l6-4 4 3 6-4" />
    </svg>
  )
}

export function AlertsIcon({ className }: IconProps) {
  return (
    <svg {...common} className={className}>
      <path d="M18 8a6 6 0 1 0-12 0c0 6-2 7-2 7h16s-2-1-2-7" />
      <path d="M10.3 20a1.8 1.8 0 0 0 3.4 0" />
    </svg>
  )
}

/** Saved-products shortcut in the header (wishlist-style heart). */
export function HeartIcon({ className }: IconProps) {
  return (
    <svg {...common} className={className}>
      <path d="M12 20.5S3.5 15.4 3.5 9.2C3.5 6.3 5.8 4 8.6 4c1.6 0 3.1.8 3.4 2.2C12.3 4.8 13.8 4 15.4 4c2.8 0 5.1 2.3 5.1 5.2 0 6.2-8.5 11.3-8.5 11.3z" />
    </svg>
  )
}

/** Price-drop alerts shortcut in the header (a bell). */
export function BellIcon({ className }: IconProps) {
  return (
    <svg {...common} className={className}>
      <path d="M18 8a6 6 0 1 0-12 0c0 6-2 7-2 7h16s-2-1-2-7" />
      <path d="M10.3 20a1.8 1.8 0 0 0 3.4 0" />
    </svg>
  )
}

/** A refresh/clock glyph for "prices updated live". */
export function RefreshIcon({ className }: IconProps) {
  return (
    <svg {...common} className={className}>
      <path d="M20 11a8 8 0 1 0-2.6 6.4" />
      <path d="M20 5v6h-6" />
    </svg>
  )
}

/** A shield with a check mark, for a trust/guarantee badge. */
export function ShieldCheckIcon({ className }: IconProps) {
  return (
    <svg {...common} className={className}>
      <path d="M12 3l7 3v6c0 4.5-3 7.5-7 9-4-1.5-7-4.5-7-9V6l7-3z" />
      <path d="M9 12l2 2 4-4" />
    </svg>
  )
}

export function ShoeIcon({ className }: IconProps) {
  return (
    <svg {...common} className={className}>
      <path d="M3 17c0-2 1.5-2.5 3-3.5 1.5-1 2.5-2.7 3-4.5.4 1 1.3 1.7 2.4 1.7.9 0 1.6-.5 2.1-1.2.6.9 2 2 4 2.3 1.7.3 3.5 1.4 3.5 3.7v2H3v-.5z" />
    </svg>
  )
}

export function PerfumeIcon({ className }: IconProps) {
  return (
    <svg {...common} className={className}>
      <path d="M10 4h4M11 4v2.5M8 6.5h8l1 3.5v9a2 2 0 0 1-2 2H9a2 2 0 0 1-2-2v-9l1-3.5z" />
      <path d="M8 12h8" />
    </svg>
  )
}

export function ApplianceIcon({ className }: IconProps) {
  return (
    <svg {...common} className={className}>
      <rect x="4" y="3" width="16" height="18" rx="2" />
      <circle cx="12" cy="13" r="4.2" />
      <path d="M8 6.5h.01M11 6.5h.01" />
    </svg>
  )
}

export function GameControllerIcon({ className }: IconProps) {
  return (
    <svg {...common} className={className}>
      <path d="M6 9h12l2 7.5a1.8 1.8 0 0 1-3.2 1.5L15 15H9l-1.8 3a1.8 1.8 0 0 1-3.2-1.5L6 9z" />
      <path d="M8.5 11.5v3M7 13h3M16 11.5h.01M18 13h.01" />
    </svg>
  )
}

export function PhoneDeviceIcon({ className }: IconProps) {
  return (
    <svg {...common} className={className}>
      <rect x="7" y="2.5" width="10" height="19" rx="2.2" />
      <path d="M11 18.5h2" />
    </svg>
  )
}

export function LaptopIcon({ className }: IconProps) {
  return (
    <svg {...common} className={className}>
      <rect x="5" y="4" width="14" height="9.5" rx="1.4" />
      <path d="M3 18.5h18l-1.6-3H4.6l-1.6 3z" />
    </svg>
  )
}

/** A play-button triangle, for the decorative Google Play badge. */
export function PlayGlyphIcon({ className }: IconProps) {
  return (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor" className={className} aria-hidden="true">
      <path d="M6 3.5v17c0 .8.9 1.3 1.6.9l14-8.5a1 1 0 0 0 0-1.8l-14-8.5C6.9 2.2 6 2.7 6 3.5z" />
    </svg>
  )
}

/** A simple apple silhouette, for the decorative App Store badge. */
export function AppleGlyphIcon({ className }: IconProps) {
  return (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor" className={className} aria-hidden="true">
      <path d="M16.4 12.2c0-2.5 2-3.7 2.1-3.8-1.1-1.6-2.9-1.9-3.5-1.9-1.5-.2-2.9.9-3.6.9-.7 0-1.9-.8-3.1-.8-1.6 0-3.1.9-3.9 2.4-1.7 2.9-.4 7.3 1.2 9.6.8 1.2 1.7 2.5 3 2.4 1.2 0 1.6-.8 3.1-.8s1.8.8 3.1.7c1.3 0 2.1-1.2 2.9-2.4.6-.9 1-1.8 1.3-2.7-1.5-.6-2.6-2-2.6-3.6z" />
      <path d="M14 3.5c.6-.7 1-1.7.9-2.6-.9.1-1.9.6-2.5 1.3-.5.6-1 1.6-.9 2.5.9.1 1.9-.4 2.5-1.2z" />
    </svg>
  )
}
