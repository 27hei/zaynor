import { useTranslation } from '../i18n/useTranslation'

interface Testimonial {
  quote: string
  lang?: 'ar' | 'en'
  name: string
  place: string
  avatarInitial: string
  avatarClass: string
  featured?: boolean
}

const TESTIMONIALS: Testimonial[] = [
  {
    quote: 'وفّرت أكثر من 600 ريال في شراء جوالي الجديد. المقارنة كانت سريعة والأسعار فعلًا محدثة.',
    name: 'نورة العتيبي',
    place: 'الرياض، السعودية',
    avatarInitial: 'ن',
    avatarClass: 'avatar-green',
  },
  {
    quote: 'ZAYNOR made it effortless to find the best deal. The price alert saved me money at exactly the right time.',
    lang: 'en',
    name: 'Ahmed Alharbi',
    place: 'Jeddah, Saudi Arabia',
    avatarInitial: 'A',
    avatarClass: 'avatar-navy',
    featured: true,
  },
  {
    quote: 'أحببت تنبيهات انخفاض الأسعار. واجهة بسيطة، ومتاجر موثوقة، وقرار شراء أسهل بكثير.',
    name: 'لولوة السالم',
    place: 'الخبر، السعودية',
    avatarInitial: 'ل',
    avatarClass: 'avatar-gold',
  },
]

/**
 * Curated testimonials matching the founder-approved design reference —
 * static editorial copy (not pulled from the real review system), same as
 * any marketing page's "what our customers say" section built before real
 * review volume exists.
 */
export function ZaynorTestimonials() {
  const { t } = useTranslation()

  return (
    <section className="testimonials" aria-labelledby="testimonials-title">
      <div className="section-heading">
        <span className="section-kicker">{t('testimonials.kicker')}</span>
        <h2 id="testimonials-title">
          {t('testimonials.titlePrefix')} <em>ZAYNOR</em>
          {t('testimonials.titleSuffix')}
        </h2>
        <p>{t('testimonials.subtitle')}</p>
      </div>

      <div className="testimonial-grid">
        {TESTIMONIALS.map((item) => (
          <article key={item.name} className={item.featured ? 'testimonial-card featured-review' : 'testimonial-card'}>
            <div className="rating" aria-label={t('testimonials.ratingLabel')}>
              ★★★★★
            </div>
            {item.lang === 'en' ? (
              <blockquote lang="en">&ldquo;{item.quote}&rdquo;</blockquote>
            ) : (
              <blockquote>«{item.quote}»</blockquote>
            )}
            <footer className="reviewer" dir={item.lang === 'en' ? 'ltr' : undefined}>
              <span className={`avatar ${item.avatarClass}`}>{item.avatarInitial}</span>
              <div>
                <strong>{item.name}</strong>
                <small>{item.place}</small>
              </div>
              <span className="verified">{item.lang === 'en' ? '✓ Verified' : '✓ موثّق'}</span>
            </footer>
          </article>
        ))}
      </div>
    </section>
  )
}
