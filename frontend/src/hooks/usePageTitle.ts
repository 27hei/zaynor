import { useEffect } from 'react'

const BASE_TITLE = 'ZAYNOR — Smart Shopping Decisions'

/** Sets the document title for the current page; restores the base on unmount. */
export function usePageTitle(title?: string) {
  useEffect(() => {
    document.title = title ? `${title} · ZAYNOR` : BASE_TITLE
    return () => {
      document.title = BASE_TITLE
    }
  }, [title])
}
