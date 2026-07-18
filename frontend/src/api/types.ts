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
  freeShipping: boolean
  deliveryDays: number | null
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

export interface UserDto {
  id: number
  email: string
  locale: string
  createdAt: string
}

export interface AuthResponse {
  token: string
  expiresAt: string
  user: UserDto
}
