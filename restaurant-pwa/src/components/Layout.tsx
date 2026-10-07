import { Link, useNavigate, useLocation } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import type { ReactNode } from 'react';

interface LayoutProps {
  children: ReactNode;
  showNav?: boolean;
}

export default function Layout({ children, showNav = true }: LayoutProps) {
  const { user, logout, isAuthenticated } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <div className="min-h-screen bg-gray-50">
      {showNav && (
        <nav className="bg-white shadow-sm border-b border-gray-200">
          <div className="max-w-6xl mx-auto px-4 py-3 flex items-center justify-between">
            <Link to="/riders" className="text-xl font-bold text-blue-700">
              Puyo Delivery
            </Link>

            <div className="flex items-center gap-4">
              {isAuthenticated ? (
                <>
                  <Link
                    to="/riders"
                    className={`px-3 py-2 rounded-lg text-sm font-medium ${
                      location.pathname === '/riders'
                        ? 'bg-blue-50 text-blue-700'
                        : 'text-gray-600 hover:bg-gray-50'
                    }`}
                  >
                    Riders
                  </Link>
                  <span className="text-sm text-gray-500 hidden md:inline">
                    {user?.fullName}
                  </span>
                  <button
                    onClick={handleLogout}
                    className="px-3 py-2 rounded-lg text-sm font-medium text-red-600 hover:bg-red-50"
                  >
                    Salir
                  </button>
                </>
              ) : (
                <Link
                  to="/login"
                  className="px-4 py-2 bg-blue-600 text-white rounded-lg text-sm font-medium hover:bg-blue-700"
                >
                  Iniciar Sesión
                </Link>
              )}
            </div>
          </div>
        </nav>
      )}
      <main>{children}</main>
    </div>
  );
}
