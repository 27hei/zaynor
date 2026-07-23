import { useRef } from 'react'
import { useNavigate } from 'react-router-dom'
import { CATEGORY_SEEDS, type CategoryKey } from '../categories'
import { useTranslation } from '../i18n/useTranslation'

// Real product-photo crops crops from the approved design reference —
// exact positions transcribed from the founder-supplied template.
const CATEGORY_IMAGE_CLASS: Partial<Record<CategoryKey, string>> = {
  electronics: 'laptop',
  phones: 'phone',
  gaming: 'game',
  appliances: 'washer',
  personalCare: 'perfume',
  fashion: 'shoe',
}

// Home shows a curated 6 (all of which have a matching reference-image
// crop); the rest stay reachable from the full Categories page.
const HOME_CATEGORY_COUNT = 6

interface HomeCategoriesProps {
  onSelect: (query: string) => void
}

/** A compact, icon-led category strip on the home page (spec Section 16: Home shows categories). */
export function HomeCategories({ onSelect }: HomeCategoriesProps) {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const railRef = useRef<HTMLDivElement>(null)
  const shown = CATEGORY_SEEDS.slice(0, HOME_CATEGORY_COUNT)

  function scrollRail(direction: 1 | -1) {
    railRef.current?.scrollBy({ left: direction * 220, behavior: 'smooth' })
  }

  return (
    <section id="categories" className="categories" aria-labelledby="categories-title">
      <h2 id="categories-title">
        <span>✦</span> {t('categories.homeTitle')}
      </h2>
      <div className="category-rail">
        <button type="button" className="rail-control" aria-label={t('categories.previous')} onClick={() => scrollRail(-1)}>
          ←
        </button>
        <div className="category-list" ref={railRef}>
          {shown.map(({ key, seed }) => (
            <button key={key} type="button" className="home-category-card" onClick={() => onSelect(seed)}>
              <span className={`category-image ${CATEGORY_IMAGE_CLASS[key] ?? ''}`} aria-hidden="true" />
              <b>{t(`category.${key}`)}</b>
            </button>
          ))}
        </div>
      </div>
      <button type="button" className="all-categories" onClick={() => navigate('/categories')}>
        {t('categories.viewAll')} <span>‹</span>
      </button>
    </section>
  )
}
