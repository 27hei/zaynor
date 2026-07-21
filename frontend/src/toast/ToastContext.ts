import { createContext } from 'react'

export type ToastKind = 'error' | 'success' | 'info'

export interface ToastItem {
  id: number
  kind: ToastKind
  message: string
}

export interface ToastContextValue {
  push: (message: string, kind?: ToastKind) => void
}

export const ToastContext = createContext<ToastContextValue | null>(null)
