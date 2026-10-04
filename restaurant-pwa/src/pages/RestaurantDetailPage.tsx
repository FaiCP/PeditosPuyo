import { useState, useEffect } from 'react';
import { useParams, Link } from 'react-router-dom';
import api from '../api/client';
import type { Restaurant, MenuItem } from '../types';

export default function RestaurantDetailPage() {
  const { id } = useParams();
  const [restaurant, setRestaurant] = useState<Restaurant | null>(null);
  const [menu, setMenu] = useState<MenuItem[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!id) return;

    Promise.all([
      api.get<Restaurant>(`/restaurants/${id}`),
      api.get<MenuItem[]>(`/restaurants/${id}/menu`),
    ])
      .then(([restaurantRes, menuRes]) => {
        setRestaurant(restaurantRes.data);
        setMenu(menuRes.data);
      })
      .catch(() => setRestaurant(null))
      .finally(() => setLoading(false));
  }, [id]);

  if (loading) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  if (!restaurant) {
    return (
      <div className="min-h-screen flex flex-col items-center justify-center p-4">
        <p className="text-gray-500 text-lg mb-4">Restaurante no encontrado.</p>
        <Link to="/" className="text-blue-600 hover:text-blue-800">
          ← Volver al catálogo
        </Link>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50">
      <div className="bg-gradient-to-r from-blue-600 to-blue-700 text-white">
        <div className="max-w-4xl mx-auto px-4 py-8">
          <Link to="/" className="text-blue-100 hover:text-white inline-block mb-4">
            ← Volver al catálogo
          </Link>
          <h1 className="text-3xl font-bold">{restaurant.name}</h1>
          <p className="text-blue-100 mt-2 flex items-center gap-2">
            <span>📍</span> {restaurant.address}
          </p>
          <a
            href={`tel:${restaurant.phone}`}
            className="inline-flex items-center gap-2 mt-3 text-blue-100 hover:text-white"
          >
            <span>📞</span> {restaurant.phone}
          </a>
        </div>
      </div>

      <div className="max-w-4xl mx-auto px-4 py-8">
        <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-6 mb-6">
          <h2 className="text-xl font-bold text-gray-900 mb-2">Cómo pedir</h2>
          <ol className="space-y-3 text-gray-700">
            <li className="flex items-start gap-3">
              <span className="bg-blue-100 text-blue-700 rounded-full w-6 h-6 flex items-center justify-center text-sm font-bold">1</span>
              <span>Llama al restaurante para confirmar tu pedido</span>
            </li>
            <li className="flex items-start gap-3">
              <span className="bg-blue-100 text-blue-700 rounded-full w-6 h-6 flex items-center justify-center text-sm font-bold">2</span>
              <span>El restaurante prepara tu orden</span>
            </li>
            <li className="flex items-start gap-3">
              <span className="bg-blue-100 text-blue-700 rounded-full w-6 h-6 flex items-center justify-center text-sm font-bold">3</span>
              <span>Un rider recoge y te entrega a domicilio</span>
            </li>
          </ol>
        </div>

        {menu.length > 0 && (
          <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
            <h2 className="text-xl font-bold text-gray-900 mb-4">Menú</h2>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              {menu.map((item) => (
                <div
                  key={item.id}
                  className="border border-gray-200 rounded-lg p-4 hover:border-blue-300 transition"
                >
                  <div className="flex justify-between items-start">
                    <h3 className="font-semibold text-gray-900">{item.name}</h3>
                    <span className="text-blue-600 font-bold">${item.price.toFixed(2)}</span>
                  </div>
                  {item.description && (
                    <p className="text-gray-600 text-sm mt-1">{item.description}</p>
                  )}
                </div>
              ))}
            </div>
          </div>
        )}

        {restaurant.menuSummary && menu.length === 0 && (
          <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
            <h2 className="text-xl font-bold text-gray-900 mb-2">Menú</h2>
            <p className="text-gray-700">{restaurant.menuSummary}</p>
          </div>
        )}
      </div>
    </div>
  );
}
