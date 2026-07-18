import { useCallback, useState } from 'react'

const STORAGE_KEY = 'zaynor.recent'
const MAX_ITEMS = 5

function read(): string[] {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    const parsed: unknown = raw ? JSON.parse(raw) : []
    return Array.isArray(parsed) ? parsed.filter((x): x is string => typeof x === 'string') : []
  } catch {
    return []
  }
}

/** The visitor's last searches, persisted locally (spec Section 16: Home shows last searches). */
export function useRecentSearches() {
  const [recent, setRecent] = useState<string[]>(read)

  const add = useCallback((query: string) => {
    setRecent((current) => {
      const next = [
        query,
        ...current.filter((q) => q.toLowerCase() !== query.toLowerCase()),
      ].slice(0, MAX_ITEMS)
      localStorage.setItem(STORAGE_KEY, JSON.stringify(next))
      return next
    })
  }, [])

  const clear = useCallback(() => {
    localStorage.removeItem(STORAGE_KEY)
    setRecent([])
  }, [])

  return { recent, add, clear }
}
