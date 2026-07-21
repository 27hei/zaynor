import { useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/useAuth'
import { useTranslation } from '../i18n/useTranslation'
import { usePageTitle } from '../hooks/usePageTitle'
import { AuthLayout } from '../components/AuthLayout'

export function LoginPage() {
  const { t } = useTranslation()
  usePageTitle(t('nav.login'))
  const { login } = useAuth()
  const navigate = useNavigate()

  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setError(null)
    setSubmitting(true)
    try {
      await login(email, password)
      navigate('/account')
    } catch (err) {
      setError((err as Error).message || t('auth.genericError'))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <AuthLayout>
      <section className="auth-card">
        <h1 className="auth-title">{t('auth.loginTitle')}</h1>
        <p className="auth-subtitle">{t('auth.loginSubtitle')}</p>

        <form className="auth-form" onSubmit={handleSubmit}>
          {error && <p className="auth-error" role="alert">{error}</p>}

          <label className="field">
            <span className="field-label">{t('auth.email')}</span>
            <input
              type="email"
              className="field-input"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder={t('auth.emailPlaceholder')}
              autoComplete="email"
              required
            />
          </label>

          <label className="field">
            <span className="field-label">{t('auth.password')}</span>
            <input
              type="password"
              className="field-input"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder={t('auth.passwordPlaceholder')}
              autoComplete="current-password"
              required
            />
          </label>

          <button type="submit" className="btn btn-primary btn-block" disabled={submitting}>
            {submitting ? t('auth.submitting') : t('auth.loginButton')}
          </button>
        </form>

        <p className="auth-switch">
          {t('auth.noAccount')} <Link to="/register">{t('nav.register')}</Link>
        </p>
      </section>
    </AuthLayout>
  )
}
