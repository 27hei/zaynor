import { useNavigate } from 'react-router-dom'
import { useTranslation } from '../i18n/useTranslation'
import {
  AnalysisIcon,
  DiscoveryIcon,
  IntelligenceIcon,
  SavingsIcon,
  AlertsIcon,
  TrustIcon,
} from '../components/icons'

// Category browsing is an early/expansion feature (spec FR10). For now each
// category seeds a search so it's genuinely useful rather than a dead link.
const CATEGORIES = [
  { key: 'electronics', icon: DiscoveryIcon, seed: 'laptop' },
  { key: 'gaming', icon: IntelligenceIcon, seed: 'PlayStation 5' },
  { key: 'phones', icon: SavingsIcon, seed: 'iPhone 15' },
  { key: 'computers', icon: AnalysisIcon, seed: 'MacBook' },
  { key: 'tv', icon: TrustIcon, seed: 'Samsung TV' },
  { key: 'appliances', icon: AlertsIcon, seed: 'air fryer' },
] as const

export function CategoriesPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()

  return (
    <section className="page-article">
      <h1 className="page-title">{t('categories.title')}</h1>
      <p className="page-subtitle">{t('categories.subtitle')}</p>

      <div className="category-grid">
        {CATEGORIES.map(({ key, icon: Icon, seed }) => (
          <button
            key={key}
            type="button"
            className="category-card"
            onClick={() => navigate(`/?q=${encodeURIComponent(seed)}`)}
          >
            <span className="category-icon">
              <Icon />
            </span>
            <span className="category-name">{t(`category.${key}`)}</span>
          </button>
        ))}
      </div>
    </section>
  )
}
