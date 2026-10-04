import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import { AuthProvider } from './context/AuthContext';
import PublicCatalogPage from './pages/PublicCatalogPage';
import RestaurantDetailPage from './pages/RestaurantDetailPage';
import LoginPage from './pages/LoginPage';
import DashboardPage from './pages/DashboardPage';
import NewDeliveryPage from './pages/NewDeliveryPage';

function App() {
  return (
    <AuthProvider>
      <Router>
        <div className="min-h-screen bg-gray-50">
          <Routes>
            <Route path="/" element={<PublicCatalogPage />} />
            <Route path="/restaurant/:id" element={<RestaurantDetailPage />} />
            <Route path="/login" element={<LoginPage />} />
            <Route path="/dashboard" element={<DashboardPage />} />
            <Route path="/new-delivery" element={<NewDeliveryPage />} />
          </Routes>
        </div>
      </Router>
    </AuthProvider>
  );
}

export default App;
