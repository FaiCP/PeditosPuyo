import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import { AuthProvider } from './context/AuthContext';
import Layout from './components/Layout';
import PublicCatalogPage from './pages/PublicCatalogPage';
import RestaurantDetailPage from './pages/RestaurantDetailPage';
import LoginPage from './pages/LoginPage';
import DashboardPage from './pages/DashboardPage';
import NewDeliveryPage from './pages/NewDeliveryPage';
import RidersPage from './pages/RidersPage';

function App() {
  return (
    <AuthProvider>
      <Router>
        <Routes>
          <Route
            path="/"
            element={
              <Layout showNav={false}>
                <PublicCatalogPage />
              </Layout>
            }
          />
          <Route
            path="/restaurant/:id"
            element={
              <Layout showNav={false}>
                <RestaurantDetailPage />
              </Layout>
            }
          />
          <Route
            path="/login"
            element={
              <Layout showNav={false}>
                <LoginPage />
              </Layout>
            }
          />
          <Route
            path="/dashboard"
            element={
              <Layout>
                <DashboardPage />
              </Layout>
            }
          />
          <Route
            path="/new-delivery"
            element={
              <Layout>
                <NewDeliveryPage />
              </Layout>
            }
          />
          <Route
            path="/riders"
            element={
              <Layout>
                <RidersPage />
              </Layout>
            }
          />
        </Routes>
      </Router>
    </AuthProvider>
  );
}

export default App;
