import { Logo } from './Logo'

export function Header() {
  return (
    <header className="header">
      <div className="header-inner">
        <div className="header-brand">
          <Logo size={34} />
          <span className="header-wordmark">ZAYNOR</span>
        </div>
        <span className="header-tagline">Smart Shopping Decisions</span>
      </div>
    </header>
  )
}
