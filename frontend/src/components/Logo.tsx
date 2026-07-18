interface LogoProps {
  size?: number
}

/** The Zaynor Z-mark (spec Section 22) as an inline SVG, so it scales crisply anywhere. */
export function Logo({ size = 36 }: LogoProps) {
  return (
    <svg
      width={size}
      height={size}
      viewBox="0 0 64 64"
      role="img"
      aria-label="Zaynor logo"
    >
      <defs>
        <linearGradient id="logo-bg" x1="0" y1="0" x2="64" y2="64" gradientUnits="userSpaceOnUse">
          <stop offset="0" stopColor="#1D9E75" />
          <stop offset="1" stopColor="#0F6E56" />
        </linearGradient>
      </defs>
      <rect width="64" height="64" rx="16" fill="url(#logo-bg)" />
      <path d="M20 20h24v6.2L28.4 43.8H44V50H19.4v-6.2L35 26.2H20z" fill="#E1F5EE" />
    </svg>
  )
}
