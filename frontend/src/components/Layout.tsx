import { Outlet } from 'react-router-dom'
import { Header } from './Header'
import { Footer } from './Footer'
import { SupportWidget } from './SupportWidget'

export function Layout() {
  return (
    <div className="page">
      <Header />
      <main className="content">
        <Outlet />
      </main>
      <Footer />
      <SupportWidget />
    </div>
  )
}
