import {
  AlertsIcon,
  AnalysisIcon,
  DiscoveryIcon,
  IntelligenceIcon,
  SavingsIcon,
  TrustIcon,
} from './icons'

const FEATURES = [
  { icon: TrustIcon, title: 'Trust', text: 'Neutral recommendations — the best deal wins, even without commission.' },
  { icon: IntelligenceIcon, title: 'Intelligence', text: 'Offers are matched and organized so you compare the same product.' },
  { icon: SavingsIcon, title: 'Savings', text: 'See exactly how much you save by buying at the lowest price.' },
  { icon: DiscoveryIcon, title: 'Discovery', text: 'One search reaches every store Zaynor tracks, instantly.' },
  { icon: AnalysisIcon, title: 'Analysis', text: 'Clear, sorted results — no more tab-switching to compare prices.' },
  { icon: AlertsIcon, title: 'Alerts', text: 'Price-drop notifications are coming — track a product, get notified.' },
]

export function FeatureHighlights() {
  return (
    <section className="features" aria-label="Why Zaynor">
      <div className="features-grid">
        {FEATURES.map(({ icon: Icon, title, text }) => (
          <div className="feature-card" key={title}>
            <div className="feature-icon">
              <Icon />
            </div>
            <h3 className="feature-title">{title}</h3>
            <p className="feature-text">{text}</p>
          </div>
        ))}
      </div>
    </section>
  )
}
