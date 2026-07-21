declare global {
  interface Window {
    dataLayer: unknown[]
  }
}

const measurementId = import.meta.env.VITE_GA_MEASUREMENT_ID as string | undefined

/**
 * Config-only activation (same pattern as the backend data sources): dormant
 * until VITE_GA_MEASUREMENT_ID is set at build time, so shipping this changes
 * nothing until a Measurement ID exists.
 */
export function initAnalytics(): void {
  if (!measurementId) return

  const script = document.createElement('script')
  script.async = true
  script.src = `https://www.googletagmanager.com/gtag/js?id=${measurementId}`
  document.head.appendChild(script)

  window.dataLayer = window.dataLayer || []
  const gtag = (...args: unknown[]) => window.dataLayer.push(args)
  gtag('js', new Date())
  gtag('config', measurementId)
}
