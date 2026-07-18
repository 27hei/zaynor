// Mirrors the read models returned by the backend (Zaynor.Application.Aggregation.Models).
// ASP.NET Core serializes with camelCase by default.

export interface AggregatedOffer {
  storeName: string
  productTitle: string
  price: number
  currency: string
  productUrl: string
  inStock: boolean
  imageUrl: string | null
  normalizedKey: string
  isLowestPrice: boolean
}

export interface Recommendation {
  bestStoreName: string
  bestPrice: number
  currency: string
  comparedStoreName: string
  comparedPrice: number
  savings: number
  message: string
}

export interface SearchResult {
  query: string
  offers: AggregatedOffer[]
  recommendation: Recommendation | null
  offerCount: number
}
