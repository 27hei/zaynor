import type { ReactNode } from 'react'
import { BrandMark } from './BrandMark'
import { TrustIcon, SavingsIcon, AlertsIcon } from './icons'
import { useTranslation } from '../i18n/useTranslation'

const POINTS = [
  { icon: TrustIcon, key: 'trust' },
  { icon: SavingsIcon, key: 'savings' },
  { icon: AlertsIcon, key: 'alerts' },
] as const

/**
 * Wraps the login/register form with a brand/value-prop panel on wide
 * screens — previously the form card floated alone on an otherwise empty
 * page. Hidden below the desktop breakpoint, where the card already fills
 * the width well on its own.
 */
export function AuthLayout({ children }: { children: ReactNode }) {
  const { t } = useTranslation()

  return (
    <div className="auth-page">
      <div className="auth-side">
        <BrandMark size={48} />
        <h2 className="auth-side-title">{t('hero.title')}</h2>
        <ul className="auth-side-points">
          {POINTS.map(({ icon: Icon, key }) => (
            <li key={key}>
              <span className="auth-side-icon">
                <Icon />
              </span>
              <span>{t(`feature.${key}.text`)}</span>
            </li>
          ))}
        </ul>
      </div>
      {children}
    </div>
  )
}
