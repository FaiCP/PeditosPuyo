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

  const getLocation = () => {
    if (navigator.geolocation) {
      navigator.geolocation.getCurrentPosition(
        (position) => {
          setLat(position.coords.latitude.toString());
          setLng(position.coords.longitude.toString());
        },
        (err) => setError('No se pudo obtener la ubicación: ' + err.message)
      );
    } else {
      setError('Geolocalización no soportada');
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError('');

    try {
      await api.post('/delivery-requests', {
        restaurantId: localStorage.getItem('restaurantId'),
        deliveryAddress: address,
        lat: parseFloat(lat),
        lng: parseFloat(lng),
        notes: notes || undefined,
      });
      navigate('/dashboard');
    } catch {
      setError('Error al crear el delivery');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="max-w-2xl mx-auto p-6">
      <h1 className="text-2xl font-bold mb-6">Nuevo Delivery</h1>

      <form onSubmit={handleSubmit} className="bg-white rounded-lg shadow-md p-6">
        <div className="mb-4">
          <label className="block text-sm font-medium text-gray-700 mb-1">Dirección de entrega</label>
          <input
            type="text"
            value={address}
            onChange={(e) => setAddress(e.target.value)}
            className="w-full p-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500"
            placeholder="Ej: Av. Principal 123"
            required
          />
        </div>

        <div className="grid grid-cols-2 gap-4 mb-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Latitud</label>
            <input
              type="text"
              value={lat}
              onChange={(e) => setLat(e.target.value)}
              className="w-full p-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500"
              placeholder="-0.1807"
              required
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Longitud</label>
            <input
              type="text"
              value={lng}
              onChange={(e) => setLng(e.target.value)}
              className="w-full p-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500"
              placeholder="-78.4678"
              required
            />
          </div>
        </div>

        <button
          type="button"
          onClick={getLocation}
          className="mb-4 text-blue-600 hover:text-blue-800 text-sm"
        >
          📍 Usar mi ubicación actual
        </button>

        <div className="mb-6">
          <label className="block text-sm font-medium text-gray-700 mb-1">Notas (opcional)</label>
          <textarea
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            className="w-full p-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500"
            rows={3}
            placeholder="Instrucciones especiales..."
          />
        </div>

        {error && <p className="text-red-500 text-sm mb-4">{error}</p>}

        <div className="flex gap-4">
          <button
            type="button"
            onClick={() => navigate('/dashboard')}
            className="flex-1 bg-gray-200 p-3 rounded-lg hover:bg-gray-300"
          >
            Cancelar
          </button>
          <button
            type="submit"
            disabled={loading}
            className="flex-1 bg-blue-600 text-white p-3 rounded-lg hover:bg-blue-700 disabled:opacity-50"
          >
            {loading ? 'Enviando...' : 'Solicitar Delivery'}
          </button>
        </div>
      </form>
    </div>
  );
}
