import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import api from '../api/client';
import type { ClaimableRestaurant } from '../types';

export default function ClaimRestaurantPage() {
  const [restaurants, setRestaurants] = useState<ClaimableRestaurant[]>([]);
  const [selectedId, setSelectedId] = useState('');
  const [fullName, setFullName] = useState('');
  const [email, setEmail] = useState('');
  const [phone, setPhone] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const [submitted, setSubmitted] = useState(false);

  useEffect(() => {
    api
      .get<ClaimableRestaurant[]>('/restaurant-claims/claimable')
      .then(({ data }) => setRestaurants(data))
      .catch(() => setError('No se pudo cargar la lista de restaurantes.'));
  }, []);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

    if (!selectedId) {
      setError('Selecciona un restaurante.');
      return;
    }

    setLoading(true);
    try {
      await api.post('/restaurant-claims', {
        restaurantId: selectedId,
        fullName,
        email,
        phone,
        password,
      });
      setSubmitted(true);
    } catch (err: any) {
      setError(err.response?.data?.error || 'No se pudo enviar la solicitud.');
    } finally {
      setLoading(false);
    }
  };

  if (submitted) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center p-4">
        <div className="bg-white rounded-xl shadow-lg p-8 max-w-md w-full text-center">
          <h1 className="text-2xl font-bold text-green-700 mb-2">Solicitud enviada</h1>
          <p className="text-gray-600 mb-6">
            Tu solicitud está pendiente de aprobación. Te contactaremos por email.
          </p>
          <Link
            to="/login"
            className="inline-block bg-blue-600 text-white px-6 py-2 rounded-lg hover:bg-blue-700"
          >
            Ir al login
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50 py-8 px-4">
      <div className="max-w-xl mx-auto bg-white rounded-xl shadow-lg p-6 md:p-8">
        <h1 className="text-2xl font-bold text-blue-700 mb-2">Reclamar restaurante</h1>
        <p className="text-gray-600 mb-6">
          Si tu restaurante ya aparece en la plataforma, selecciónalo y envía una solicitud.
        </p>

        {error && (
          <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg mb-4 text-sm">
            {error}
          </div>
        )}

        {restaurants.length === 0 ? (
          <p className="text-gray-500 text-center py-8">
            No hay restaurantes disponibles para reclamar en este momento.
          </p>
        ) : (
          <form onSubmit={handleSubmit} className="space-y-5">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Restaurante *</label>
              <select
                value={selectedId}
                onChange={(e) => setSelectedId(e.target.value)}
                className="w-full p-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500"
                required
              >
                <option value="">Selecciona...</option>
                {restaurants.map((r) => (
                  <option key={r.id} value={r.id}>
                    {r.name} — {r.address || r.phone}
                  </option>
                ))}
              </select>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Nombre completo *</label>
              <input
                type="text"
                value={fullName}
                onChange={(e) => setFullName(e.target.value)}
                className="w-full p-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500"
                required
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Email *</label>
              <input
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                className="w-full p-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500"
                required
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Teléfono *</label>
              <input
                type="tel"
                value={phone}
                onChange={(e) => setPhone(e.target.value)}
                className="w-full p-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500"
                required
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Contraseña *</label>
              <input
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                className="w-full p-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500"
                required
                minLength={6}
              />
            </div>

            <button
              type="submit"
              disabled={loading}
              className="w-full bg-blue-600 text-white p-3 rounded-lg hover:bg-blue-700 disabled:opacity-50 font-medium"
            >
              {loading ? 'Enviando...' : 'Enviar solicitud'}
            </button>
          </form>
        )}

        <div className="mt-6 text-center text-sm text-gray-500 space-y-2">
          <p>
            ¿No encuentras tu restaurante?{' '}
            <Link to="/register-restaurant" className="text-blue-600 hover:underline">
              Registrar uno nuevo
            </Link>
          </p>
          <p>
            <Link to="/login" className="text-blue-600 hover:underline">
              Volver al login
            </Link>
          </p>
        </div>
      </div>
    </div>
  );
}
