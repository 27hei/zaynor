import type { AggregatedOffer } from '../api/types'
import { formatPrice } from '../format'
import { productArtFor } from '../productImage'
import { useTranslation } from '../i18n/useTranslation'

interface ProductSummaryProps {
  query: string
  offers: AggregatedOffer[]
}

/**
 * The product header above results: illustration (or the real product image
 * when a feed provides one), title, and the at-a-glance market summary.
 */
export function ProductSummary({ query, offers }: ProductSummaryProps) {
  const { t } = useTranslation()

  const best = offers.find((o) => o.isLowestPrice) ?? offers[0]
  const image = best?.imageUrl ?? productArtFor(query)

  return (
    <div className="product-summary">
      <img className="product-summary-art" src={image} alt="" aria-hidden="true" />
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
      </div>
    </div>
  )
}
