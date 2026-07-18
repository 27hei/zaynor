import { Wordmark } from './Wordmark'

export function Footer() {
  return (
    <footer className="footer">
      <div className="footer-inner">
        <Wordmark className="footer-brand" />
        <p className="footer-note">
          Zaynor does not sell products — it helps you find the best price before you buy.
        </p>
        <p className="footer-links">
          <span>About</span>
          <span aria-hidden="true">·</span>
          <span>How It Works</span>
          <span aria-hidden="true">·</span>
          <span>Privacy Policy</span>
        </p>
        <p className="footer-copyright">© {new Date().getFullYear()} Zaynor. All rights reserved.</p>
      </div>
    </footer>
  )
}
