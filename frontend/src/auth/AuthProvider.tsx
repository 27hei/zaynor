import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import { fetchCurrentUser, loginUser, registerUser } from '../api/client'
import type { UserDto } from '../api/types'
import { AuthContext } from './AuthContext'

const TOKEN_KEY = 'zaynor.token'

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<UserDto | null>(null)
  const [loading, setLoading] = useState(true)

  // On mount, restore the session from a stored token (if still valid).
  useEffect(() => {
    const token = localStorage.getItem(TOKEN_KEY)
    if (!token) {
      setLoading(false)
      return
    }

    let cancelled = false
    fetchCurrentUser(token)
      .then((restored) => {
        if (cancelled) return
        if (restored) {
          setUser(restored)
        } else {
          localStorage.removeItem(TOKEN_KEY)
        }
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })

    return () => {
      cancelled = true
    }
  }, [])

  const login = useCallback(async (email: string, password: string) => {
    const auth = await loginUser(email, password)
    localStorage.setItem(TOKEN_KEY, auth.token)
    setUser(auth.user)
  }, [])

  const register = useCallback(async (email: string, password: string, locale: string) => {
    const auth = await registerUser(email, password, locale)
    localStorage.setItem(TOKEN_KEY, auth.token)
    setUser(auth.user)
  }, [])

  const logout = useCallback(() => {
    localStorage.removeItem(TOKEN_KEY)
    setUser(null)
  }, [])

  const value = useMemo(
    () => ({ user, loading, login, register, logout }),
    [user, loading, login, register, logout],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
