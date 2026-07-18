import { useState } from 'react'
import { Logo } from './Logo'

// The real logo asset. Drop the app-icon (rounded-square Z mark) into
// frontend/public as one of these names and it is used automatically; until
// then the drawn SVG below is shown as a fallback.
const ASSET_SOURCES = ['/zaynor-mark.svg', '/zaynor-mark.png']

interface BrandMarkProps {
  size?: number
  /** Used only by the SVG fallback (real asset ignores it). */
  detailed?: boolean
}

/**
 * Renders the official Zaynor logo from a file in /public, falling back to the
 * built-in vector mark if the file isn't present yet. This keeps the header and
 * hero on-brand the moment the real artwork is added — no code change needed.
 */
export function BrandMark({ size = 36, detailed = false }: BrandMarkProps) {
  const [srcIndex, setSrcIndex] = useState(0)

  if (srcIndex >= ASSET_SOURCES.length) {
    return <Logo size={size} detailed={detailed} />
  }

  return (
    <img
      src={ASSET_SOURCES[srcIndex]}
      width={size}
      height={size}
      alt="Zaynor"
      className="brand-mark-img"
      onError={() => setSrcIndex((index) => index + 1)}
      style={{ width: size, height: size, objectFit: 'contain', display: 'block' }}
    />
  )
}
