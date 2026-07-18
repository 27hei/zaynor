// Small stroke icons for the feature highlights, matching the brand's icon
// set (ثقة Trust, ذكاء Intelligence, توفير Savings, اكتشاف Discovery,
// تحليل Analysis, تنبيهات Alerts). Inline SVG so there's no icon-library
// dependency for a handful of glyphs.

type IconProps = { className?: string }

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
