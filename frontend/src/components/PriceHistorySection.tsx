import { useEffect, useState } from 'react'
import { getPriceHistory } from '../api/client'
import type { PriceHistoryResponse } from '../api/types'
import { storeLineColor } from '../storeBrand'
import { useTranslation } from '../i18n/useTranslation'

interface PriceHistorySectionProps {
  query: string
}

interface Series {
  storeName: string
  points: { time: number; price: number }[]
}

const WIDTH = 640
const HEIGHT = 220
const PAD = { top: 16, right: 16, bottom: 28, left: 56 }

function buildSeries(data: PriceHistoryResponse): Series[] {
  const byStore = new Map<string, { time: number; price: number }[]>()
  for (const point of data.points) {
    const list = byStore.get(point.storeName) ?? []
    list.push({ time: Date.parse(point.recordedAt), price: point.price })
    byStore.set(point.storeName, list)
  }
  return [...byStore.entries()].map(([storeName, points]) => ({ storeName, points }))
}

/**
 * Progressive-disclosure price history (competitive analysis table stakes #5,
 * principle H): collapsed by default, and honest about sparse data while the
 * PriceHistory table is still accumulating.
 */
export function PriceHistorySection({ query }: PriceHistorySectionProps) {
  const { t, lang } = useTranslation()
  const [open, setOpen] = useState(false)
  const [data, setData] = useState<PriceHistoryResponse | null>(null)
  const [failed, setFailed] = useState(false)

  useEffect(() => {
    if (!open || data || failed) return
    getPriceHistory(query)
      .then(setData)
      .catch(() => setFailed(true))
  }, [open, data, failed, query])

  const series = data ? buildSeries(data) : []
  const allPoints = series.flatMap((s) => s.points)
  const timestamps = [...new Set(allPoints.map((p) => p.time))].sort((a, b) => a - b)
  const hasChart = timestamps.length >= 2

  // Scales. With a single timestamp the chart collapses to dots at center.
  const prices = allPoints.map((p) => p.price)
  const minPrice = Math.min(...(prices.length ? prices : [0]))
  const maxPrice = Math.max(...(prices.length ? prices : [1]))
  const priceSpan = maxPrice - minPrice || maxPrice * 0.1 || 1
  const minTime = timestamps[0] ?? 0
  const timeSpan = (timestamps[timestamps.length - 1] ?? 1) - minTime || 1

  const x = (time: number) => PAD.left + ((time - minTime) / timeSpan) * (WIDTH - PAD.left - PAD.right)
  const y = (price: number) =>
    HEIGHT - PAD.bottom - ((price - minPrice) / priceSpan) * (HEIGHT - PAD.top - PAD.bottom)

  const dateFormat = new Intl.DateTimeFormat(lang === 'ar' ? 'ar' : 'en', {
    month: 'short',
    day: 'numeric',
  })
  const priceFormat = new Intl.NumberFormat(lang === 'ar' ? 'ar' : 'en', {
    maximumFractionDigits: 0,
  })

  return (
    <section className="history-section">
      <button type="button" className="history-toggle" onClick={() => setOpen((v) => !v)}>
        {open ? t('history.hide') : t('history.show')}
      </button>

      {open && (
        <div className="history-panel">
          {failed && <p className="hint hint-error">{t('account.loadError')}</p>}

          {data && !hasChart && !failed && (
            <p className="history-note">{t('history.accumulating')}</p>
          )}

          {data && hasChart && (
            <>
              <div className="history-chart-wrap" dir="ltr">
                <svg
                  viewBox={`0 0 ${WIDTH} ${HEIGHT}`}
                  className="history-chart"
                  role="img"
                  aria-label={t('history.title')}
                >
                  {/* Price gridlines: min and max */}
                  <line
                    x1={PAD.left} x2={WIDTH - PAD.right} y1={y(maxPrice)} y2={y(maxPrice)}
                    stroke="#e6ebe8" strokeDasharray="4 4"
                  />
                  <line
                    x1={PAD.left} x2={WIDTH - PAD.right} y1={y(minPrice)} y2={y(minPrice)}
                    stroke="#e6ebe8" strokeDasharray="4 4"
                  />
                  <text x={PAD.left - 8} y={y(maxPrice) + 4} textAnchor="end" className="history-axis">
                    {priceFormat.format(maxPrice)}
                  </text>
                  <text x={PAD.left - 8} y={y(minPrice) + 4} textAnchor="end" className="history-axis">
                    {priceFormat.format(minPrice)}
                  </text>
                  <text x={PAD.left} y={HEIGHT - 8} textAnchor="start" className="history-axis">
                    {dateFormat.format(minTime)}
                  </text>
                  <text x={WIDTH - PAD.right} y={HEIGHT - 8} textAnchor="end" className="history-axis">
                    {dateFormat.format(timestamps[timestamps.length - 1])}
                  </text>

                  {series.map((s) => {
                    const color = storeLineColor(s.storeName)
                    const path = s.points
                      .map((p, i) => `${i === 0 ? 'M' : 'L'}${x(p.time)},${y(p.price)}`)
                      .join(' ')
                    return (
                      <g key={s.storeName}>
                        {s.points.length > 1 && (
                          <path d={path} fill="none" stroke={color} strokeWidth="2.5" />
                        )}
                        {s.points.map((p, i) => (
                          <circle key={i} cx={x(p.time)} cy={y(p.price)} r="3.5" fill={color} />
                        ))}
                      </g>
                    )
                  })}
                </svg>
              </div>

              <div className="history-legend">
                {series.map((s) => (
                  <span className="history-legend-item" key={s.storeName}>
                    <span
                      className="history-legend-dot"
                      style={{ background: storeLineColor(s.storeName) }}
                    />
                    {s.storeName}
                  </span>
                ))}
              </div>
            </>
          )}
        </div>
      )}
    </section>
  )
}
