import { Outlet } from 'react-router-dom'
import { Header } from './Header'
import { Footer } from './Footer'

export function Layout() {
  return (
    <div className="page">
      <Header />
      <main className="content">
        <Outlet />
      </main>
      <Footer />
    </div>
  )
}
