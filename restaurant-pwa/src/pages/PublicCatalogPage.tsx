import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import api from '../api/client';
import type { Restaurant } from '../types';

export default function PublicCatalogPage() {
  const [restaurants, setRestaurants] = useState<Restaurant[]>([]);
  const [search, setSearch] = useState('');
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    api
      .get<Restaurant[]>('/restaurants')
      .then(({ data }) => setRestaurants(data))
      .catch(() => setRestaurants([]))
      .finally(() => setLoading(false));
  }, []);

  const filtered = restaurants.filter(
    (r) =>
      r.name.toLowerCase().includes(search.toLowerCase()) ||
      r.menuSummary?.toLowerCase().includes(search.toLowerCase()) ||
      r.address.toLowerCase().includes(search.toLowerCase())
  );

  return (
    <div className="min-h-screen bg-gradient-to-b from-blue-50 to-white">
      <div className="max-w-6xl mx-auto px-4 py-8">
        <header className="text-center mb-8">
          <h1 className="text-4xl font-bold text-blue-700 mb-2">Puyo Delivery</h1>
          <p className="text-gray-600 text-lg">Encuentra tu restaurante favorito y pide a domicilio</p>

          <div className="mt-6 max-w-md mx-auto relative">
            <input
              type="text"
              placeholder="Buscar restaurante, plato o dirección..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="w-full p-4 pl-12 border border-gray-300 rounded-xl focus:ring-2 focus:ring-blue-500 focus:border-transparent shadow-sm"
            />
            <span className="absolute left-4 top-1/2 -translate-y-1/2 text-gray-400">🔍</span>
          </div>
        </header>

        {loading ? (
          <div className="text-center py-12">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto"></div>
            <p className="text-gray-500 mt-4">Cargando restaurantes...</p>
          </div>
        ) : filtered.length === 0 ? (
          <div className="text-center py-12">
            <p className="text-gray-500 text-lg">No se encontraron restaurantes.</p>
            {search && (
              <button
                onClick={() => setSearch('')}
                className="text-blue-600 hover:text-blue-800 mt-2"
              >
                Limpiar búsqueda
              </button>
            )}
          </div>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {filtered.map((r) => (
              <div
                key={r.id}
                className="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden hover:shadow-lg transition"
              >
                <div className="bg-gradient-to-r from-blue-500 to-blue-600 h-32 flex items-center justify-center">
                  <span className="text-5xl">🍽️</span>
                </div>
                <div className="p-5">
                  <h2 className="text-xl font-bold text-gray-900 mb-1">{r.name}</h2>
                  <p className="text-gray-600 text-sm flex items-center gap-1 mb-2">
                    <span>📍</span> {r.address}
                  </p>
                  {r.menuSummary && (
                    <p className="text-gray-500 text-sm line-clamp-2 mb-3">{r.menuSummary}</p>
                  )}
                  <div className="flex gap-3 mt-4">
                    <Link
                      to={`/restaurant/${r.id}`}
                      className="flex-1 bg-blue-600 text-white text-center py-2 rounded-lg hover:bg-blue-700 font-medium text-sm"
                    >
                      Ver menú
                    </Link>
                    <a
                      href={`tel:${r.phone}`}
                      className="flex-1 bg-gray-100 text-gray-700 text-center py-2 rounded-lg hover:bg-gray-200 font-medium text-sm"
                    >
                      📞 Llamar
                    </a>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}

        <footer className="mt-12 text-center text-gray-400 text-sm">
          <p>¿Eres restaurante? <Link to="/login" className="text-blue-600 hover:text-blue-800">Únete como partner</Link></p>
        </footer>
      </div>
    </div>
  );
}
