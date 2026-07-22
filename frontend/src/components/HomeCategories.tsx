import { useNavigate } from 'react-router-dom'
import { CATEGORY_SEEDS, type CategoryKey } from '../categories'
import {
  ShoeIcon,
  PerfumeIcon,
  ApplianceIcon,
  GameControllerIcon,
  PhoneDeviceIcon,
  LaptopIcon,
  AnalysisIcon,
  TrustIcon,
  type IconComponent,
} from './icons'
import { useTranslation } from '../i18n/useTranslation'

const CATEGORY_ICONS: Record<CategoryKey, IconComponent> = {
  fashion: ShoeIcon,
  personalCare: PerfumeIcon,
  appliances: ApplianceIcon,
  gaming: GameControllerIcon,
  phones: PhoneDeviceIcon,
  electronics: LaptopIcon,
  computers: AnalysisIcon,
  tv: TrustIcon,
}

// Home shows a curated 6; the rest stay reachable from the full Categories page.
const HOME_CATEGORY_COUNT = 6

interface HomeCategoriesProps {
  onSelect: (query: string) => void
}

/** A compact, icon-led category strip on the home page (spec Section 16: Home shows categories). */
export function HomeCategories({ onSelect }: HomeCategoriesProps) {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const shown = CATEGORY_SEEDS.slice(0, HOME_CATEGORY_COUNT)

  return (
    <section className="home-categories" aria-label={t('categories.homeTitle')}>
      <h2 className="home-categories-title">✦ {t('categories.homeTitle')}</h2>
      <div className="home-categories-row">
        {shown.map(({ key, seed }) => {
          const Icon = CATEGORY_ICONS[key]
          return (
            <button key={key} type="button" className="home-category-chip" onClick={() => onSelect(seed)}>
              <span className="home-category-chip-icon">
                <Icon />
              </span>
              <span>{t(`category.${key}`)}</span>
            </button>
          )
        })}
      </div>
      <button type="button" className="home-categories-viewall" onClick={() => navigate('/categories')}>
        {t('categories.viewAll')}
      </button>
    </section>
  )
}
