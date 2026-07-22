import type {
  AdminSupportTicketDetailDto,
  AdminSupportTicketDto,
  AlertDto,
  AuthResponse,
  PriceHistoryResponse,
  ReviewDto,
  SavedProductDto,
  SearchResult,
  SupportMessageDto,
  SupportTicketDetailDto,
  SupportTicketDto,
  UserDto,
} from './types'
import { getToken } from '../auth/token'

// Configurable per environment (VITE_API_URL at build time); localhost for dev.
const API_BASE_URL = (import.meta.env.VITE_API_URL as string | undefined) ?? 'http://localhost:5286'

/** Calls the backend search endpoint and returns the aggregated result. */
export async function searchProducts(
  query: string,
  signal?: AbortSignal,
): Promise<SearchResult> {
  const url = `${API_BASE_URL}/api/search?q=${encodeURIComponent(query)}`
  const response = await fetch(url, { signal })

  if (!response.ok) {
    throw new Error(`Search failed (${response.status})`)
  }

  return (await response.json()) as SearchResult
}

/**
 * "Search by photo": uploads the image, gets back the product name our
 * reverse-image lookup recognized. The caller then runs that name through
 * the normal searchProducts() — image search shares the exact same
 * aggregation pipeline as a typed query, not a separate results path.
 */
export async function searchByImage(file: File, signal?: AbortSignal): Promise<string> {
  const form = new FormData()
  form.append('image', file)

  const response = await fetch(`${API_BASE_URL}/api/search/by-image`, {
    method: 'POST',
    body: form,
    signal,
  })

  if (!response.ok) {
    throw new Error(await readError(response, `Image search failed (${response.status})`))
  }

  const body = (await response.json()) as { query: string }
  return body.query
}

/** Autocomplete suggestions from products Zaynor has seen. */
export async function getSuggestions(query: string, signal?: AbortSignal): Promise<string[]> {
  const url = `${API_BASE_URL}/api/search/suggestions?q=${encodeURIComponent(query)}`
  const response = await fetch(url, { signal })

  if (!response.ok) {
    return []
  }

  return (await response.json()) as string[]
}

/** The accumulated price history for a product (table stakes #5). */
export async function getPriceHistory(query: string): Promise<PriceHistoryResponse> {
  const url = `${API_BASE_URL}/api/search/history?q=${encodeURIComponent(query)}`
  const response = await fetch(url)

  if (!response.ok) {
    throw new Error(`History failed (${response.status})`)
  }

  return (await response.json()) as PriceHistoryResponse
}

/** Outbound store link routed through click tracking (spec Sections 10/20). */
export function outboundUrl(url: string, store: string, product: string, signature?: string | null): string {
  const sig = signature ? `&sig=${encodeURIComponent(signature)}` : ''
  return `${API_BASE_URL}/api/out?u=${encodeURIComponent(url)}&store=${encodeURIComponent(store)}&product=${encodeURIComponent(product)}${sig}`
}

export interface CatalogSummary {
  name: string
  category: string
  lowestPrice: number
  currency: string
  offerCount: number
  image: string | null
}

/** Covered products with real lowest prices (FR10 category browsing). */
export async function getCatalog(): Promise<CatalogSummary[]> {
  const response = await fetch(`${API_BASE_URL}/api/catalog`)
  if (!response.ok) return []
  return (await response.json()) as CatalogSummary[]
}

/** Extracts a human-readable error message from an error response body. */
async function readError(response: Response, fallback: string): Promise<string> {
  try {
    const body = (await response.json()) as { error?: string }
    return body.error ?? fallback
  } catch {
    return fallback
  }
}

export async function registerUser(
  email: string,
  password: string,
  locale: string,
): Promise<AuthResponse> {
  const response = await fetch(`${API_BASE_URL}/api/auth/register`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password, locale }),
  })

  if (!response.ok) {
    throw new Error(await readError(response, 'Registration failed.'))
  }

  return (await response.json()) as AuthResponse
}

export async function loginUser(email: string, password: string): Promise<AuthResponse> {
  const response = await fetch(`${API_BASE_URL}/api/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password }),
  })

  if (!response.ok) {
    throw new Error(await readError(response, 'Invalid email or password.'))
  }

  return (await response.json()) as AuthResponse
}

/** Fetches the current user for a stored token; returns null if the token is invalid/expired. */
export async function fetchCurrentUser(token: string): Promise<UserDto | null> {
  const response = await fetch(`${API_BASE_URL}/api/auth/me`, {
    headers: { Authorization: `Bearer ${token}` },
  })

  if (!response.ok) {
    return null
  }

  return (await response.json()) as UserDto
}

/** Performs an authenticated request using the stored session token. */
async function authFetch(path: string, init?: RequestInit): Promise<Response> {
  const token = getToken()
  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...init,
    headers: {
      ...(init?.headers ?? {}),
      Authorization: `Bearer ${token ?? ''}`,
    },
  })

  if (!response.ok) {
    throw new Error(await readError(response, `Request failed (${response.status})`))
  }

  return response
}

// --- Saved products (spec FR9) ---

export async function getSavedProducts(): Promise<SavedProductDto[]> {
  const response = await authFetch('/api/saved')
  return (await response.json()) as SavedProductDto[]
}

export async function saveProduct(productName: string): Promise<SavedProductDto> {
  const response = await authFetch('/api/saved', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ productName }),
  })
  return (await response.json()) as SavedProductDto
}

export async function removeSavedProduct(id: number): Promise<void> {
  await authFetch(`/api/saved/${id}`, { method: 'DELETE' })
}

// --- Price-drop alerts (spec FR8) ---

export async function getAlerts(): Promise<AlertDto[]> {
  const response = await authFetch('/api/alerts')
  return (await response.json()) as AlertDto[]
}

export async function createAlert(
  productName: string,
  priceBaseline: number | null,
  currency: string | null,
): Promise<AlertDto> {
  const response = await authFetch('/api/alerts', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ productName, priceBaseline, currency }),
  })
  return (await response.json()) as AlertDto
}

export async function removeAlert(id: number): Promise<void> {
  await authFetch(`/api/alerts/${id}`, { method: 'DELETE' })
}

// --- Store reviews ---

export async function getStoreReviews(storeName: string): Promise<ReviewDto[]> {
  const response = await fetch(`${API_BASE_URL}/api/reviews?storeName=${encodeURIComponent(storeName)}`)
  if (!response.ok) return []
  return (await response.json()) as ReviewDto[]
}

/** A small curated highlight for the homepage — not a filter on what's visible on a store's own review list. */
export async function getFeaturedReviews(): Promise<ReviewDto[]> {
  const response = await fetch(`${API_BASE_URL}/api/reviews/featured`)
  if (!response.ok) return []
  return (await response.json()) as ReviewDto[]
}

export async function submitReview(
  storeName: string,
  rating: number,
  comment: string,
  displayName: string | null,
): Promise<ReviewDto> {
  const response = await authFetch('/api/reviews', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ storeName, rating, comment, displayName }),
  })
  return (await response.json()) as ReviewDto
}

// --- Admin: reviews ---

export async function getAllReviews(): Promise<ReviewDto[]> {
  const response = await authFetch('/api/admin/reviews')
  return (await response.json()) as ReviewDto[]
}

export async function replyToReview(reviewId: number, reply: string): Promise<ReviewDto> {
  const response = await authFetch(`/api/admin/reviews/${reviewId}/reply`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ reply }),
  })
  return (await response.json()) as ReviewDto
}

// --- Support tickets ---

export async function getMyTickets(): Promise<SupportTicketDto[]> {
  const response = await authFetch('/api/support/tickets')
  return (await response.json()) as SupportTicketDto[]
}

export async function createTicket(subject: string, message: string): Promise<SupportTicketDto> {
  const response = await authFetch('/api/support/tickets', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ subject, message }),
  })
  return (await response.json()) as SupportTicketDto
}

export async function getMyTicket(id: number): Promise<SupportTicketDetailDto> {
  const response = await authFetch(`/api/support/tickets/${id}`)
  return (await response.json()) as SupportTicketDetailDto
}

export async function addTicketMessage(id: number, body: string): Promise<SupportMessageDto> {
  const response = await authFetch(`/api/support/tickets/${id}/messages`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ body }),
  })
  return (await response.json()) as SupportMessageDto
}

// --- Admin: support tickets ---

export async function getAllTickets(): Promise<AdminSupportTicketDto[]> {
  const response = await authFetch('/api/admin/support/tickets')
  return (await response.json()) as AdminSupportTicketDto[]
}

export async function getAdminTicket(id: number): Promise<AdminSupportTicketDetailDto> {
  const response = await authFetch(`/api/admin/support/tickets/${id}`)
  return (await response.json()) as AdminSupportTicketDetailDto
}

export async function addAdminReply(id: number, body: string): Promise<SupportMessageDto> {
  const response = await authFetch(`/api/admin/support/tickets/${id}/messages`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ body }),
  })
  return (await response.json()) as SupportMessageDto
}

export async function closeTicket(id: number): Promise<void> {
  await authFetch(`/api/admin/support/tickets/${id}/close`, { method: 'POST' })
}
