import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';

export default function LandingPage() {
  const navigate = useNavigate();
  const [token, setToken] = useState('');

  const handleCustomerAccess = (e: React.FormEvent) => {
    e.preventDefault();
    const t = token.trim();
    if (t) navigate(`/p/${t}`);
  };

  return (
    <div className="min-h-screen bg-gradient-to-b from-blue-50 to-white py-12 px-4">
      <div className="max-w-3xl mx-auto text-center">
        <h1 className="text-4xl font-bold text-blue-700 mb-4">Puyo Delivery</h1>
        <p className="text-lg text-gray-600 mb-10">
          Plataforma de pedidos locales: restaurantes, riders y clientes en un solo lugar.
        </p>

        <div className="grid md:grid-cols-3 gap-6 mb-10">
          <div className="bg-white rounded-xl shadow p-6">
            <div className="text-3xl mb-3">🍽️</div>
            <h2 className="text-lg font-semibold text-gray-800 mb-2">Soy cliente</h2>
            <p className="text-sm text-gray-500 mb-4">
              Ingresa con el enlace que te compartió el restaurante o la empresa.
            </p>
            <form onSubmit={handleCustomerAccess} className="space-y-2">
              <input
                type="text"
                value={token}
                onChange={(e) => setToken(e.target.value)}
                placeholder="Código del enlace"
                className="w-full p-2 border border-gray-300 rounded-lg text-sm"
              />
              <button
                type="submit"
                className="w-full bg-blue-600 text-white py-2 rounded-lg text-sm hover:bg-blue-700"
              >
                Ver catálogo
              </button>
            </form>
          </div>

          <div className="bg-white rounded-xl shadow p-6">
            <div className="text-3xl mb-3">🍔</div>
            <h2 className="text-lg font-semibold text-gray-800 mb-2">Soy restaurante</h2>
            <p className="text-sm text-gray-500 mb-4">
              Registra tu restaurante o reclama uno que ya está en la plataforma.
            </p>
            <div className="space-y-2">
              <Link
                to="/register-restaurant"
                className="block w-full bg-green-600 text-white py-2 rounded-lg text-sm hover:bg-green-700"
              >
                Registrar restaurante
              </Link>
              <Link
                to="/claim-restaurant"
                className="block w-full bg-white border border-green-600 text-green-600 py-2 rounded-lg text-sm hover:bg-green-50"
              >
                Reclamar restaurante
              </Link>
            </div>
          </div>

          <div className="bg-white rounded-xl shadow p-6">
            <div className="text-3xl mb-3">🛵</div>
            <h2 className="text-lg font-semibold text-gray-800 mb-2">Soy rider / admin</h2>
            <p className="text-sm text-gray-500 mb-4">
              Accede al panel de administración de riders o reclamos.
            </p>
            <Link
              to="/login"
              className="block w-full bg-blue-600 text-white py-2 rounded-lg text-sm hover:bg-blue-700"
            >
              Iniciar sesión
            </Link>
          </div>
        </div>

        <p className="text-sm text-gray-500">
          ¿Tienes un restaurante y no aparece?{' '}
          <Link to="/register-restaurant" className="text-blue-600 hover:underline">
            Regístralo aquí
          </Link>
        </p>
      </div>
    </div>
  );
}
