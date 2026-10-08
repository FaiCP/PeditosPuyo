import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider } from './context/AuthContext';
import Layout from './components/Layout';
import LoginPage from './pages/LoginPage';
import RegisterRestaurantPage from './pages/RegisterRestaurantPage';
import ClaimRestaurantPage from './pages/ClaimRestaurantPage';
import RestaurantDashboardPage from './pages/RestaurantDashboardPage';
import RestaurantClaimsPage from './pages/RestaurantClaimsPage';
import RidersPage from './pages/RidersPage';
import CustomerLayout from './pages/customer/CustomerLayout';
import CustomerCatalogPage from './pages/customer/CustomerCatalogPage';
import CustomerMenuPage from './pages/customer/CustomerMenuPage';
import CustomerNewOrderPage from './pages/customer/CustomerNewOrderPage';
import CustomerOrderPage from './pages/customer/CustomerOrderPage';
import CustomerOrdersPage from './pages/customer/CustomerOrdersPage';

function App() {
  return (
    <AuthProvider>
      <Router>
        <Routes>
          <Route
            path="/p/:token"
            element={<CustomerLayout />}
          >
            <Route index element={<CustomerCatalogPage />} />
            <Route path="r/:restaurantId" element={<CustomerMenuPage />} />
            <Route path="nuevo" element={<CustomerNewOrderPage />} />
            <Route path="mis-pedidos" element={<CustomerOrdersPage />} />
            <Route path="order/:orderId" element={<CustomerOrderPage />} />
          </Route>
          <Route
            path="/login"
            element={
              <Layout showNav={false}>
                <LoginPage />
              </Layout>
            }
          />
          <Route
            path="/register-restaurant"
            element={
              <Layout showNav={false}>
                <RegisterRestaurantPage />
              </Layout>
            }
          />
          <Route
            path="/claim-restaurant"
            element={
              <Layout showNav={false}>
                <ClaimRestaurantPage />
              </Layout>
            }
          />
          <Route
            path="/restaurant/dashboard"
            element={
              <Layout>
                <RestaurantDashboardPage />
              </Layout>
            }
          />
          <Route
            path="/claims"
            element={
              <Layout>
                <RestaurantClaimsPage />
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
          <Route
            path="/"
            element={<Navigate to="/login" replace />}
          />
        </Routes>
      </Router>
    </AuthProvider>
  );
}

export default App;
