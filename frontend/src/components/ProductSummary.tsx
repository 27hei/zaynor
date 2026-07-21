import { useState } from 'react'
import type { AggregatedOffer } from '../api/types'
import { formatPrice } from '../format'
import { productArtFor } from '../productImage'
import { useTranslation } from '../i18n/useTranslation'
import { StoreRating } from './StoreRating'

interface ProductSummaryProps {
  query: string
  offers: AggregatedOffer[]
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
export function ProductSummary({ query, offers }: ProductSummaryProps) {
  const { t } = useTranslation()

  const best = offers.find((o) => o.isLowestPrice) ?? offers[0]
  const fallbackArt = productArtFor(query)

  // Distinct real photos across every store's listing (several stores often
  // reuse the same manufacturer photo, so dedupe by URL).
  const gallery = [...new Set(offers.map((o) => o.imageUrl).filter((url): url is string => !!url))].slice(
    0,
    MAX_THUMBNAILS,
  )

  const [selectedImage, setSelectedImage] = useState<string | null>(null)
  const activeImage = selectedImage ?? best?.imageUrl ?? fallbackArt

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
        {best && (
          <p className="product-summary-meta">
            {t('summary.meta', {
              count: offers.length,
              price: formatPrice(best.price, best.currency),
            })}
          </p>
        )}
        {best?.rating != null && (
          <StoreRating rating={best.rating} ratingCount={best.ratingCount} />
        )}
      </div>
    </div>
  )
}
