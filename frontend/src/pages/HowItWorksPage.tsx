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
    </article>
  )
}
