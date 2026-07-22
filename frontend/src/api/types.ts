// Mirrors the read models returned by the backend (Zaynor.Application.Aggregation.Models).
// ASP.NET Core serializes with camelCase by default.

/** Rich detail fields already fetched during search (GoogleShoppingDataSource only) — null for every other source, never fabricated. */
export interface ProductDetails {
  images: string[] | null
  brand: string | null
  description: string | null
  specifications: string[] | null
  storeHighlights: string[] | null
}

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
  /** True when this specific store's link currently carries an active affiliate tag. */
  hasAffiliateLink: boolean
  /** Proves to /api/out this link came from a real search result — required for stores outside its static known-domain list. */
  signature: string | null
  productDetails: ProductDetails | null
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
  /** Set only when a colloquial Arabic brand spelling was corrected before searching (e.g. "سامسنج" → "Samsung"). */
  correctedQuery: string | null
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
  isAdmin: boolean
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

/** A customer's rating + comment about a specific store — always public, admin may reply. */
export interface ReviewDto {
  id: number
  storeName: string
  displayName: string | null
  rating: number
  comment: string
  createdAt: string
  adminReply: string | null
  adminReplyAt: string | null
}

export interface SupportMessageDto {
  id: number
  isFromAdmin: boolean
  body: string
  createdAt: string
}

export interface SupportTicketDto {
  id: number
  subject: string
  isClosed: boolean
  createdAt: string
  updatedAt: string
  messageCount: number
}

export interface SupportTicketDetailDto {
  id: number
  subject: string
  isClosed: boolean
  createdAt: string
  messages: SupportMessageDto[]
}

export interface AdminSupportTicketDto extends SupportTicketDto {
  userEmail: string
}

export interface AdminSupportTicketDetailDto extends SupportTicketDetailDto {
  userEmail: string
}

/** A customer's rating + comment about Zaynor itself — always public. */
export interface SiteReviewDto {
  id: number
  displayName: string | null
  rating: number
  comment: string
  createdAt: string
}
