import { useState, useEffect } from 'react';
import { useParams, Link } from 'react-router-dom';
import api from '../api/client';
import type { Restaurant } from '../types';

export default function RestaurantDetailPage() {
  const { id } = useParams();
  const [restaurant, setRestaurant] = useState<Restaurant | null>(null);

  useEffect(() => {
    api.get<Restaurant>(`/restaurants/${id}`).then(({ data }) => setRestaurant(data));
  }, [id]);

  return (
    <div className="max-w-4xl mx-auto p-6">
      <Link to="/" className="text-blue-600 hover:text-blue-800">
        ← Volver al catálogo
      </Link>

      {restaurant && (
        <div className="mt-4">
          <h1 className="text-3xl font-bold">{restaurant.name}</h1>
          <p className="text-gray-600 mt-2">{restaurant.address}</p>
          <a
            href={`tel:${restaurant.phone}`}
            className="inline-block mt-2 text-blue-600 hover:text-blue-800 font-medium"
          >
            Llamar: {restaurant.phone}
          </a>

          {restaurant.menuSummary && (
            <div className="mt-4 p-4 bg-gray-100 rounded-lg">
              <h2 className="text-lg font-semibold mb-2">Menú</h2>
              <p className="text-gray-700">{restaurant.menuSummary}</p>
            </div>
          )}

          <div className="mt-6 p-4 bg-blue-50 rounded-lg border border-blue-200">
            <p className="text-blue-800">
              Para hacer un pedido, llama al restaurante directamente. Una vez confirmado, el restaurante solicitará el delivery desde su panel.
            </p>
          </div>
        </div>
      )}
    </div>
  );
}
