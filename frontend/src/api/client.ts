import type { AuthResponse, SearchResult, UserDto } from './types'

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
