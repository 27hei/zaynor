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
  rating: number | null
  ratingCount: number | null
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
  isDemoData: boolean
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

export interface PriceHistoryPoint {
  storeName: string
  price: number
  recordedAt: string
}

export interface PriceHistoryResponse {
  productName: string | null
  points: PriceHistoryPoint[]
}

export interface SavedProductDto {
  id: number
  productName: string
  savedAt: string
}

export interface AlertDto {
  id: number
  productName: string
  targetCondition: string
  isActive: boolean
  createdAt: string
}
