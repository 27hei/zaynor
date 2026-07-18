import { useNavigate } from 'react-router-dom'
import { useTranslation } from '../i18n/useTranslation'
import { usePageTitle } from '../hooks/usePageTitle'
import { CATEGORY_SEEDS, type CategoryKey } from '../categories'
import {
  AnalysisIcon,
  DiscoveryIcon,
  IntelligenceIcon,
  SavingsIcon,
  AlertsIcon,
  TrustIcon,
} from '../components/icons'

const CATEGORY_ICONS: Record<CategoryKey, typeof DiscoveryIcon> = {
  electronics: DiscoveryIcon,
  gaming: IntelligenceIcon,
  phones: SavingsIcon,
  computers: AnalysisIcon,
  tv: TrustIcon,
  appliances: AlertsIcon,
}

// Category browsing is an early/expansion feature (spec FR10). For now each
// category seeds a search so it's genuinely useful rather than a dead link.
export function CategoriesPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()

  usePageTitle(t('nav.categories'))

  return (
    <section className="page-article">
      <h1 className="page-title">{t('categories.title')}</h1>
      <p className="page-subtitle">{t('categories.subtitle')}</p>

      <div className="category-grid">
        {CATEGORY_SEEDS.map(({ key, seed }) => {
          const Icon = CATEGORY_ICONS[key]
          return (
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
          )
        })}
      </div>
    </section>
  )
}
