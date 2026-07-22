import { Navigate } from 'react-router-dom'
import type { ReactNode } from 'react'
import { useAuth } from './useAuth'

/** Renders children only for signed-in admins; redirects to login if signed out, home if not an admin. */
export function AdminRoute({ children }: { children: ReactNode }) {
  const { user, loading } = useAuth()

  if (loading) {
    return <p className="hint">…</p>
  }

  if (!user) {
    return <Navigate to="/login" replace />
  }

  if (!user.isAdmin) {
    return <Navigate to="/" replace />
  }

  return <>{children}</>
}
