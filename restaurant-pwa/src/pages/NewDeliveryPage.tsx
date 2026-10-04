import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import api from '../api/client';

export default function NewDeliveryPage() {
  const navigate = useNavigate();
  const [address, setAddress] = useState('');
  const [lat, setLat] = useState('');
  const [lng, setLng] = useState('');
  const [notes, setNotes] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [locationLoading, setLocationLoading] = useState(false);

  const getLocation = () => {
    setLocationLoading(true);
    setError('');

    if (!navigator.geolocation) {
      setError('Geolocalización no soportada en este navegador');
      setLocationLoading(false);
      return;
    }

    navigator.geolocation.getCurrentPosition(
      (position) => {
        setLat(position.coords.latitude.toFixed(6));
        setLng(position.coords.longitude.toFixed(6));
        setLocationLoading(false);
      },
      (err) => {
        setError('No se pudo obtener la ubicación: ' + err.message);
        setLocationLoading(false);
      },
      { enableHighAccuracy: true, timeout: 10000 }
    );
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError('');

    try {
      const user = JSON.parse(localStorage.getItem('user') || '{}');
      await api.post('/delivery-requests', {
        restaurantId: user.restaurantId,
        deliveryAddress: address,
        lat: parseFloat(lat),
        lng: parseFloat(lng),
        notes: notes || undefined,
      });
      navigate('/dashboard');
    } catch {
      setError('Error al crear el delivery. Verifica tus datos.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="max-w-2xl mx-auto p-4 md:p-6">
      <h1 className="text-2xl font-bold text-gray-900 mb-2">Nuevo Delivery</h1>
      <p className="text-gray-600 mb-6">Completa los datos para solicitar un delivery</p>

      <form onSubmit={handleSubmit} className="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
        <div className="mb-5">
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Dirección de entrega *
          </label>
          <input
            type="text"
            value={address}
            onChange={(e) => setAddress(e.target.value)}
            className="w-full p-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            placeholder="Ej: Av. Principal 123, Puyo"
            required
          />
        </div>

        <div className="mb-4">
          <button
            type="button"
            onClick={getLocation}
            disabled={locationLoading}
            className="flex items-center gap-2 text-blue-600 hover:text-blue-800 text-sm font-medium disabled:opacity-50"
          >
            {locationLoading ? (
              <>
                <span className="animate-spin inline-block w-4 h-4 border-2 border-blue-600 border-t-transparent rounded-full"></span>
                Obteniendo ubicación...
              </>
            ) : (
              <>📍 Usar mi ubicación actual</>
            )}
          </button>
        </div>

        <div className="grid grid-cols-2 gap-4 mb-5">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">Latitud *</label>
            <input
              type="number"
              step="any"
              value={lat}
              onChange={(e) => setLat(e.target.value)}
              className="w-full p-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              placeholder="-0.180700"
              required
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">Longitud *</label>
            <input
              type="number"
              step="any"
              value={lng}
              onChange={(e) => setLng(e.target.value)}
              className="w-full p-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              placeholder="-78.467800"
              required
            />
          </div>
        </div>

        <div className="mb-6">
          <label className="block text-sm font-medium text-gray-700 mb-2">Notas (opcional)</label>
          <textarea
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            className="w-full p-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            rows={3}
            placeholder="Instrucciones especiales, referencias, etc..."
          />
        </div>

        {error && (
          <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg mb-4 text-sm">
            {error}
          </div>
        )}

        <div className="flex gap-4">
          <button
            type="button"
            onClick={() => navigate('/dashboard')}
            className="flex-1 bg-gray-200 text-gray-700 p-3 rounded-lg hover:bg-gray-300 font-medium"
          >
            Cancelar
          </button>
          <button
            type="submit"
            disabled={loading}
            className="flex-1 bg-blue-600 text-white p-3 rounded-lg hover:bg-blue-700 disabled:opacity-50 font-medium"
          >
            {loading ? 'Enviando...' : 'Solicitar Delivery'}
          </button>
        </div>
      </form>
    </div>
  );
}
