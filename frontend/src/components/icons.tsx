// Small stroke icons for the feature highlights (spec Section 22: Trust,
// Intelligence, Savings, Discovery, Analysis, Alerts). Inline SVG so there's
// no icon-library dependency for a handful of glyphs.

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

export function IntelligenceIcon({ className }: IconProps) {
  return (
    <svg {...common} className={className}>
      <path d="M12 3a4 4 0 0 0-4 4v1.2A4 4 0 0 0 6 12a4 4 0 0 0 1 2.6V17a2 2 0 0 0 2 2h6a2 2 0 0 0 2-2v-2.4A4 4 0 0 0 18 12a4 4 0 0 0-2-3.8V7a4 4 0 0 0-4-4z" />
      <path d="M9 21h6" />
    </svg>
  )
}

export function SavingsIcon({ className }: IconProps) {
  return (
    <svg {...common} className={className}>
      <path d="M20 12 12.6 4.6a2 2 0 0 0-1.4-.6H5a1 1 0 0 0-1 1v6.2a2 2 0 0 0 .6 1.4L12 20l8-8z" />
      <circle cx="9" cy="9" r="1.4" />
    </svg>
  )
}

export function DiscoveryIcon({ className }: IconProps) {
  return (
    <svg {...common} className={className}>
      <circle cx="11" cy="11" r="7" />
      <path d="m21 21-4.3-4.3" />
    </svg>
  )
}

export function AnalysisIcon({ className }: IconProps) {
  return (
    <svg {...common} className={className}>
      <path d="M4 20V10M12 20V4M20 20v-7" />
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
