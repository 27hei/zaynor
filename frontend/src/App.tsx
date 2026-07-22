import { Routes, Route } from 'react-router-dom'
import './App.css'
import { Layout } from './components/Layout'
import { HomePage } from './pages/HomePage'
import { ProductPage } from './pages/ProductPage'
import { ProductDetailPage } from './pages/ProductDetailPage'
import { AboutPage } from './pages/AboutPage'
import { HowItWorksPage } from './pages/HowItWorksPage'
import { PrivacyPage } from './pages/PrivacyPage'
import { TermsPage } from './pages/TermsPage'
import { CategoriesPage } from './pages/CategoriesPage'
import { LoginPage } from './pages/LoginPage'
import { RegisterPage } from './pages/RegisterPage'
import { AccountPage } from './pages/AccountPage'
import { SupportPage } from './pages/SupportPage'
import { SupportTicketPage } from './pages/SupportTicketPage'
import { AdminDashboardPage } from './pages/AdminDashboardPage'
import { AdminTicketsPage } from './pages/AdminTicketsPage'
import { AdminTicketThreadPage } from './pages/AdminTicketThreadPage'
import { AdminReviewsPage } from './pages/AdminReviewsPage'
import { NotFoundPage } from './pages/NotFoundPage'
import { ProtectedRoute } from './auth/ProtectedRoute'
import { AdminRoute } from './auth/AdminRoute'

function App() {
  return (
    <Routes>
      <Route element={<Layout />}>
        <Route index element={<HomePage />} />
        <Route path="product" element={<ProductPage />} />
        <Route path="product/details" element={<ProductDetailPage />} />
        <Route path="categories" element={<CategoriesPage />} />
        <Route path="how-it-works" element={<HowItWorksPage />} />
        <Route path="about" element={<AboutPage />} />
        <Route path="privacy" element={<PrivacyPage />} />
        <Route path="terms" element={<TermsPage />} />
        <Route path="login" element={<LoginPage />} />
        <Route path="register" element={<RegisterPage />} />
        <Route
          path="account"
          element={
            <ProtectedRoute>
              <AccountPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="support"
          element={
            <ProtectedRoute>
              <SupportPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="support/:id"
          element={
            <ProtectedRoute>
              <SupportTicketPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="admin"
          element={
            <AdminRoute>
              <AdminDashboardPage />
            </AdminRoute>
          }
        />
        <Route
          path="admin/tickets"
          element={
            <AdminRoute>
              <AdminTicketsPage />
            </AdminRoute>
          }
        />
        <Route
          path="admin/tickets/:id"
          element={
            <AdminRoute>
              <AdminTicketThreadPage />
            </AdminRoute>
          }
        />
        <Route
          path="admin/reviews"
          element={
            <AdminRoute>
              <AdminReviewsPage />
            </AdminRoute>
          }
        />
        <Route path="*" element={<NotFoundPage />} />
      </Route>
    </Routes>
  )
}

export default App
