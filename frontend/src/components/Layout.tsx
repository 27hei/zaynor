import { Outlet } from 'react-router-dom'
import { Header } from './Header'
import { Footer } from './Footer'
import { SupportWidget } from './SupportWidget'
import { PromoBar } from './PromoBar'

export function Layout() {
  return (
    <div className="page">
      <PromoBar />
      <Header />
      <main className="content">
        <Outlet />
      </main>
      <Footer />
      <SupportWidget />
    </div>
  )
}
