import { useState } from 'react'
import { Link, useLocation } from 'react-router-dom'
import type { AggregatedOffer } from '../api/types'
import { outboundUrl } from '../api/client'
import { formatPrice, shippingLabel } from '../format'
import { productArtFor } from '../productImage'
import { useTranslation } from '../i18n/useTranslation'
import { usePageTitle } from '../hooks/usePageTitle'
import { StoreLogo } from '../components/StoreLogo'
import { StoreRating } from '../components/StoreRating'
import { ReviewsSection } from '../components/ReviewsSection'

interface ProductDetailLocationState {
  offer: AggregatedOffer
  query: string
}

/**
 * Shown before a visitor leaves Zaynor for a specific store's offer — full
 * detail (images, specs, this store's own price/shipping/stock) so the "Go
 * to Store" click is an informed one. Reached only by clicking an offer row
 * in OfferList, which passes the already-fetched offer via router state —
 * no extra fetch, since GoogleShoppingDataSource already pulled this data
 * during the search itself. A direct visit/refresh has no state to read, so
 * it falls back to a simple message rather than guessing at a re-fetch.
 */
export function ProductDetailPage() {
  const { t } = useTranslation()
  const location = useLocation()
  const state = location.state as ProductDetailLocationState | null
  const [selectedImage, setSelectedImage] = useState<string | null>(null)

  usePageTitle(state?.offer.productTitle)

  if (!state) {
    return (
      <section className="product-detail-fallback">
        <p className="hint">{t('productDetail.fallbackBody')}</p>
        <Link to="/" className="btn-primary">
          {t('productDetail.fallbackAction')}
        </Link>
      </section>
    )
  }

  const { offer, query } = state
  const details = offer.productDetails
  const images =
    details?.images && details.images.length > 0
      ? details.images
      : offer.imageUrl
        ? [offer.imageUrl]
        : [productArtFor(offer.productTitle)]
  const activeImage = selectedImage ?? images[0]
  const shipping = shippingLabel(offer, t)

  return (
    <section className="product-detail">
      <Link to={`/product?q=${encodeURIComponent(query)}`} className="product-detail-back">
        {t('productDetail.backToResults')}
      </Link>

      <div className="product-detail-gallery">
        <img className="product-detail-main-image" src={activeImage} alt="" aria-hidden="true" />
        {images.length > 1 && (
          <div className="product-detail-thumbs">
            {images.map((url) => (
              <button
                key={url}
                type="button"
                className={
                  url === activeImage ? 'product-detail-thumb product-detail-thumb-active' : 'product-detail-thumb'
                }
                onClick={() => setSelectedImage(url)}
                aria-label={t('summary.viewPhoto')}
              >
                <img src={url} alt="" aria-hidden="true" loading="lazy" />
              </button>
            ))}
          </div>
        )}
      </div>

      <div className="product-detail-info">
        <h1 className="product-detail-title">{offer.productTitle}</h1>
        {details?.brand && <p className="product-detail-brand">{t('productDetail.brand', { brand: details.brand })}</p>}
        {details?.description && <p className="product-detail-description">{details.description}</p>}
        {details?.specifications && details.specifications.length > 0 && (
          <div className="product-detail-specs">
            <h2 className="product-detail-section-title">{t('productDetail.specifications')}</h2>
            <ul>
              {details.specifications.map((spec) => (
                <li key={spec}>{spec}</li>
              ))}
            </ul>
          </div>
        )}

        <div className="product-detail-store-card">
          <div className="product-detail-store-row">
            <StoreLogo storeName={offer.storeName} />
            <span className="product-detail-store-name">{offer.storeName}</span>
            {offer.rating != null && <StoreRating rating={offer.rating} ratingCount={offer.ratingCount} />}
            {offer.hasAffiliateLink && (
              <span className="offer-affiliate-badge" title={t('offer.affiliateHint')}>
                <svg width="11" height="11" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
                  <path d="M12 21s-7.5-4.6-10-9.2C.5 8.4 2.3 5 5.8 5c1.9 0 3.5 1 4.5 2.6C11.3 6 12.9 5 14.8 5c3.5 0 5.3 3.4 3.8 6.8C19.5 16.4 12 21 12 21z" />
                </svg>
                {t('offer.affiliateBadge')}
              </span>
            )}
          </div>
          <span className="product-detail-price">{formatPrice(offer.price, offer.currency)}</span>
          {!offer.inStock && <span className="offer-oos">{t('results.outOfStock')}</span>}
          {shipping && <span className="offer-shipping">{shipping}</span>}
          {details?.storeHighlights && details.storeHighlights.length > 0 && (
            <ul className="product-detail-highlights">
              {details.storeHighlights.map((h) => (
                <li key={h}>{h}</li>
              ))}
            </ul>
          )}

          <a
            className="btn-primary product-detail-cta"
            href={outboundUrl(offer.productUrl, offer.storeName, offer.productTitle, offer.signature)}
            target="_blank"
            rel="noopener noreferrer sponsored"
          >
            {t('results.goToStore')}
          </a>
        </div>
      </div>

      <ReviewsSection storeName={offer.storeName} />
    </section>
  )
}
