import { useCallback, useRef, useState, type ReactNode } from 'react'
import { ToastContext, type ToastItem, type ToastKind } from './ToastContext'

const DURATION_MS = 5000

function ToastGlyph({ kind }: { kind: ToastKind }) {
  const common = {
    width: 18,
    height: 18,
    viewBox: '0 0 24 24',
    fill: 'none',
    stroke: 'currentColor',
    strokeWidth: 2,
    strokeLinecap: 'round' as const,
    strokeLinejoin: 'round' as const,
  }

  if (kind === 'error') {
    return (
      <svg {...common} aria-hidden="true">
        <circle cx="12" cy="12" r="9" />
        <path d="M12 8v5M12 16h.01" />
      </svg>
    )
  }

  if (kind === 'success') {
    return (
      <svg {...common} aria-hidden="true">
        <circle cx="12" cy="12" r="9" />
        <path d="m8.5 12.5 2.5 2.5 4.5-5" />
      </svg>
    )
  }

  return (
    <svg {...common} aria-hidden="true">
      <circle cx="12" cy="12" r="9" />
      <path d="M12 8h.01M12 11v5" />
    </svg>
  )
}

/**
 * Transient notifications, replacing static inline error banners that used
 * to sit permanently in the page flow until the user navigated away.
 */
export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<ToastItem[]>([])
  const nextId = useRef(0)

  const dismiss = useCallback((id: number) => {
    setToasts((current) => current.filter((t) => t.id !== id))
  }, [])

  const push = useCallback(
    (message: string, kind: ToastKind = 'info') => {
      const id = nextId.current++
      setToasts((current) => [...current, { id, kind, message }])
      window.setTimeout(() => dismiss(id), DURATION_MS)
    },
    [dismiss],
  )

  return (
    <ToastContext.Provider value={{ push }}>
      {children}
      <div className="toast-stack" role="status" aria-live="polite">
        {toasts.map((t) => (
          <div
            key={t.id}
            className={`toast toast-${t.kind}`}
            onClick={() => dismiss(t.id)}
            role="button"
            tabIndex={0}
          >
            <ToastGlyph kind={t.kind} />
            <span>{t.message}</span>
          </div>
        ))}
      </div>
    </ToastContext.Provider>
  )
}
