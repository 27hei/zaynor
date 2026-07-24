import { useState } from 'react'
import type { AggregatedOffer, Recommendation } from '../api/types'
import { formatPrice } from '../format'
import { productArtFor } from '../productImage'
import { useTranslation } from '../i18n/useTranslation'
import { StoreRating } from './StoreRating'

interface ProductSummaryProps {
  query: string
  offers: AggregatedOffer[]
  recommendation: Recommendation | null
  totalCount: number
}

const MAX_THUMBNAILS = 6

/**
 * The product header above results: illustration (or the real product image
 * when a feed provides one), title, and the at-a-glance market summary. When
 * more than one store's own photo is available, they're shown as a small
 * gallery — real photos from real listings, not a stock image set.
 * The rating shown is the best-priced offer's own rating (a real, per-store
 * figure) — never an invented cross-store average.
 */
export function ProductSummary({ query, offers, recommendation, totalCount }: ProductSummaryProps) {
  const { t } = useTranslation()

  // The price/store count line reads off `recommendation` (always computed
  // from the FULL result, every page) rather than scanning `offers` — with
  // pagination, the true cheapest offer isn't guaranteed to be on this page,
  // so a page-scoped scan could understate or misname the best deal.
  const bestOnThisPage = offers.find((o) => o.isLowestPrice) ?? offers[0]
  const fallbackArt = productArtFor(query)

  // Distinct real photos across every store's listing on this page (several
  // stores often reuse the same manufacturer photo, so dedupe by URL) — the
  // gallery/rating below are page-scoped, a minor, acceptable simplification
  // since they're illustrative rather than the deal itself.
  const gallery = [...new Set(offers.map((o) => o.imageUrl).filter((url): url is string => !!url))].slice(
    0,
    MAX_THUMBNAILS,
  )

  const [selectedImage, setSelectedImage] = useState<string | null>(null)
  const activeImage = selectedImage ?? bestOnThisPage?.imageUrl ?? fallbackArt

  return (
    <div className="product-summary">
      <div className="product-summary-media">
        <img className="product-summary-art" src={activeImage} alt="" aria-hidden="true" />
        {gallery.length > 1 && (
          <div className="product-summary-thumbs">
            {gallery.map((url) => (
              <button
                key={url}
                type="button"
                className={url === activeImage ? 'product-summary-thumb product-summary-thumb-active' : 'product-summary-thumb'}
                onClick={() => setSelectedImage(url)}
                aria-label={t('summary.viewPhoto')}
              >
                <img src={url} alt="" aria-hidden="true" loading="lazy" />
              </button>
            ))}
          </div>
        )}
      </div>
      <div className="product-summary-info">
        <h2 className="product-summary-title">{query}</h2>
        {recommendation && (
          <p className="product-summary-meta">
            {t('summary.meta', {
              count: totalCount,
              price: formatPrice(recommendation.bestPrice, recommendation.currency),
            })}
          </p>
        )}
        {bestOnThisPage?.rating != null && (
          <StoreRating rating={bestOnThisPage.rating} ratingCount={bestOnThisPage.ratingCount} />
        )}
      </div>
    </div>
  )
}
