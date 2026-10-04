import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import api from '../api/client';
import type { Restaurant } from '../types';

export default function PublicCatalogPage() {
  const [restaurants, setRestaurants] = useState<Restaurant[]>([]);
  const [search, setSearch] = useState('');

  useEffect(() => {
    api.get<Restaurant[]>('/restaurants').then(({ data }) => setRestaurants(data));
  }, []);

  const filtered = restaurants.filter(
    (r) =>
      r.name.toLowerCase().includes(search.toLowerCase()) ||
      r.menuSummary?.toLowerCase().includes(search.toLowerCase())
  );

  return (
    <div className="max-w-6xl mx-auto p-6">
      <header className="mb-8">
        <h1 className="text-3xl font-bold text-blue-700">Puyo Delivery</h1>
        <p className="text-gray-600 mt-2">Encuentra tu restaurante favorito</p>
        <input
          type="text"
          placeholder="Buscar restaurante o plato..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="w-full mt-4 p-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500"
        />
      </header>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        {filtered.map((r) => (
          <Link
            key={r.id}
            to={`/restaurant/${r.id}`}
            className="bg-white rounded-lg shadow-md p-4 hover:shadow-lg transition"
          >
            <h2 className="text-xl font-semibold">{r.name}</h2>
            <p className="text-gray-600 text-sm mt-1">{r.address}</p>
            {r.menuSummary && (
              <p className="text-gray-500 text-sm mt-2 line-clamp-2">{r.menuSummary}</p>
            )}
            <a
              href={`tel:${r.phone}`}
              className="inline-block mt-3 text-blue-600 hover:text-blue-800 font-medium"
              onClick={(e) => e.stopPropagation()}
            >
              Llamar: {r.phone}
            </a>
          </Link>
        ))}
      </div>

      {filtered.length === 0 && (
        <p className="text-center text-gray-500 mt-12">No se encontraron restaurantes.</p>
      )}
    </div>
  );
}
