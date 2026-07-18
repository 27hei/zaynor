import type {
  AlertDto,
  AuthResponse,
  PriceHistoryResponse,
  SavedProductDto,
  SearchResult,
  UserDto,
} from './types'
import { getToken } from '../auth/token'

const API_BASE_URL = 'http://localhost:5286'

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
