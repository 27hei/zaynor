import { CATEGORY_SEEDS } from '../categories'
import { useTranslation } from '../i18n/useTranslation'

interface HomeCategoriesProps {
  onSelect: (query: string) => void
}

/** A compact category strip on the home page (spec Section 16: Home shows categories). */
export function HomeCategories({ onSelect }: HomeCategoriesProps) {
  const { t } = useTranslation()

  return (
    <section className="home-categories" aria-label={t('categories.title')}>
      <h2 className="home-categories-title">{t('categories.title')}</h2>
      <div className="home-categories-row">
        {CATEGORY_SEEDS.map(({ key, seed }) => (
          <button
            key={key}
            type="button"
            className="popular-chip"
            onClick={() => onSelect(seed)}
          >
            {t(`category.${key}`)}
          </button>
        ))}
      </div>
    </section>
  )
}
