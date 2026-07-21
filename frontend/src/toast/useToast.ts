import { useContext } from 'react'
import { ToastContext } from './ToastContext'

/** Push a transient notification instead of a static inline error banner. */
export function useToast() {
  const ctx = useContext(ToastContext)
  if (!ctx) {
    throw new Error('useToast must be used within a ToastProvider')
  }
  return ctx
}
