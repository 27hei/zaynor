import { useTranslation } from '../i18n/useTranslation'

const STEPS = ['step1', 'step2', 'step3', 'step4'] as const

export function HowItWorksPage() {
  const { t } = useTranslation()

  return (
    <article className="page-article">
      <h1 className="page-title">{t('how.title')}</h1>
      <ol className="steps">
        {STEPS.map((step, index) => (
          <li className="step" key={step}>
            <span className="step-number">{index + 1}</span>
            <div>
              <h2 className="step-title">{t(`how.${step}.title`)}</h2>
              <p className="step-text">{t(`how.${step}.text`)}</p>
            </div>
          </li>
        ))}
      </ol>

      {/* Visible "how we make money" transparency — trust is the product
          (competitive analysis Section 6.D). */}
      <section className="transparency">
        <h2 className="transparency-title">{t('how.moneyTitle')}</h2>
        <p className="transparency-text">{t('how.moneyText')}</p>
      </section>
    </article>
  )
}
