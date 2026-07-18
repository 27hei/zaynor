import type { SearchResult } from './types'

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
