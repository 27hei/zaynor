import { Logo } from './Logo'
import { Wordmark } from './Wordmark'

export function Header() {
  return (
    <header className="header">
      <div className="header-inner">
        <div className="header-brand">
          <Logo size={34} />
          <Wordmark className="header-wordmark" />
        </div>
        <span className="header-tagline">Smart Shopping Decisions</span>
      </div>
    </header>
  )
}
