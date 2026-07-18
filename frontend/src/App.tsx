import { useEffect, useState } from 'react'
import './App.css'

const API_BASE_URL = 'http://localhost:5286'

type HealthStatus = { status: string; service: string }

function App() {
  const [health, setHealth] = useState<HealthStatus | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    fetch(`${API_BASE_URL}/api/health`)
      .then((res) => {
        if (!res.ok) throw new Error(`Request failed: ${res.status}`)
        return res.json() as Promise<HealthStatus>
      })
      .then(setHealth)
      .catch((err: Error) => setError(err.message))
  }, [])

  return (
    <main className="shell">
      <h1>ZAYNOR</h1>
      <p className="tagline">Smart Shopping Decisions</p>
      <p className="status">
        {error && <span className="status-error">Backend unreachable: {error}</span>}
        {!error && !health && <span>Checking backend…</span>}
        {health && (
          <span className="status-ok">
            Backend connected — {health.service} is {health.status}
          </span>
        )}
      </p>
    </main>
  )
}

export default App
