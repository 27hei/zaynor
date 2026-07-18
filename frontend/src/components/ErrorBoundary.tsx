import { Component, type ReactNode } from 'react'

interface ErrorBoundaryState {
  hasError: boolean
}

/**
 * Last-resort error screen so a runtime error never leaves the user staring at
 * a blank white page. Deliberately uses static bilingual text (no i18n hooks)
 * because it must render even when the providers themselves fail.
 */
export class ErrorBoundary extends Component<{ children: ReactNode }, ErrorBoundaryState> {
  state: ErrorBoundaryState = { hasError: false }

  static getDerivedStateFromError(): ErrorBoundaryState {
    return { hasError: true }
  }

  render() {
    if (this.state.hasError) {
      return (
        <div className="error-screen">
          <p className="error-screen-title">حدث خطأ غير متوقع · Something went wrong</p>
          <button
            type="button"
            className="error-screen-button"
            onClick={() => window.location.reload()}
          >
            إعادة التحميل · Reload
          </button>
        </div>
      )
    }

    return this.props.children
  }
}
