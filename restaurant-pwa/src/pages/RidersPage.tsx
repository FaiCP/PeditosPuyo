import { useEffect, useState } from 'react';
import api from '../api/client';
import { useAuth } from '../context/AuthContext';
import type { Rider } from '../types';

export default function RidersPage() {
  const { user } = useAuth();
  const [riders, setRiders] = useState<Rider[]>([]);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState({ fullName: '', phone: '', vehiclePlate: '', email: '', password: '' });
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    fetchRiders();
  }, []);

  const fetchRiders = async () => {
    try {
      const { data } = await api.get<Rider[]>('/riders');
      setRiders(data);
    } catch (error) {
      console.error('Error fetching riders:', error);
    } finally {
      setLoading(false);
    }
  };

  const createRider = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    setError('');
    try {
      await api.post('/riders', form);
      setShowForm(false);
      setForm({ fullName: '', phone: '', vehiclePlate: '', email: '', password: '' });
      fetchRiders();
    } catch (err: any) {
      setError(err.response?.data?.error ?? 'Error al crear el rider');
    } finally {
      setSaving(false);
    }
  };

  const toggleOnline = async (rider: Rider) => {
    try {
      await api.put(`/riders/${rider.id}/status`, { isOnline: !rider.isOnline });
      fetchRiders();
    } catch (error) {
      console.error('Error updating rider:', error);
    }
  };

  return (
    <div className="max-w-6xl mx-auto p-4 md:p-6">
      <header className="flex justify-between items-center mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Gestión de Riders</h1>
          <p className="text-gray-600">Riders de {user?.fullName}</p>
        </div>
        <div className="flex gap-2">
          <button
            onClick={() => setShowForm(true)}
            className="bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700 font-medium text-sm"
          >
            + Nuevo rider
          </button>
          <button
            onClick={fetchRiders}
            className="bg-gray-200 px-4 py-2 rounded-lg hover:bg-gray-300 font-medium text-sm"
          >
            ↻ Actualizar
          </button>
        </div>
      </header>

      {showForm && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4" onClick={() => setShowForm(false)}>
          <form
            onSubmit={createRider}
            onClick={(e) => e.stopPropagation()}
            className="bg-white rounded-xl shadow-lg p-6 w-full max-w-md space-y-4"
          >
            <h2 className="text-lg font-bold text-gray-900">Nuevo rider</h2>
            {error && <p className="text-red-600 text-sm bg-red-50 rounded-lg p-2">{error}</p>}
            <input
              required
              placeholder="Nombre completo"
              value={form.fullName}
              onChange={(e) => setForm({ ...form, fullName: e.target.value })}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm"
            />
            <input
              required
              type="email"
              placeholder="Email (login del rider)"
              value={form.email}
              onChange={(e) => setForm({ ...form, email: e.target.value })}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm"
            />
            <input
              required
              placeholder="Teléfono"
              value={form.phone}
              onChange={(e) => setForm({ ...form, phone: e.target.value })}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm"
            />
            <input
              placeholder="Placa del vehículo"
              value={form.vehiclePlate}
              onChange={(e) => setForm({ ...form, vehiclePlate: e.target.value })}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm"
            />
            <input
              required
              type="password"
              minLength={6}
              placeholder="Contraseña (mín. 6, con dígito)"
              value={form.password}
              onChange={(e) => setForm({ ...form, password: e.target.value })}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm"
            />
            <div className="flex gap-2 pt-2">
              <button
                type="button"
                onClick={() => setShowForm(false)}
                className="flex-1 bg-gray-200 text-gray-700 py-2 rounded-lg hover:bg-gray-300 font-medium text-sm"
              >
                Cancelar
              </button>
              <button
                type="submit"
                disabled={saving}
                className="flex-1 bg-blue-600 text-white py-2 rounded-lg hover:bg-blue-700 font-medium text-sm disabled:opacity-50"
              >
                {saving ? 'Creando...' : 'Crear rider'}
              </button>
            </div>
          </form>
        </div>
      )}

      {loading ? (
        <div className="text-center py-12">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto"></div>
        </div>
      ) : riders.length === 0 ? (
        <div className="text-center py-12 bg-white rounded-lg border border-gray-200">
          <p className="text-gray-500 text-lg">No hay riders registrados.</p>
          <p className="text-gray-400 text-sm mt-2">Usa "+ Nuevo rider" para crear uno con su cuenta de login.</p>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {riders.map((rider) => (
            <div
              key={rider.id}
              className="bg-white rounded-lg shadow-sm border border-gray-200 p-4"
            >
              <div className="flex justify-between items-start mb-3">
                <div>
                  <h3 className="font-semibold text-gray-900">{rider.fullName}</h3>
                  <p className="text-sm text-gray-500">{rider.vehiclePlate || 'Sin placa'}</p>
                </div>
                <div className="flex flex-col items-end gap-1">
                  <span
                    className={`px-2 py-1 rounded-full text-xs font-medium ${
                      rider.isOnline
                        ? 'bg-green-100 text-green-800'
                        : 'bg-gray-100 text-gray-600'
                    }`}
                  >
                    {rider.isOnline ? 'Online' : 'Offline'}
                  </span>
                  {rider.isBusy && (
                    <span className="px-2 py-1 rounded-full text-xs font-medium bg-yellow-100 text-yellow-800">
                      Ocupado
                    </span>
                  )}
                </div>
              </div>

              <div className="space-y-1 text-sm text-gray-600 mb-3">
                <p>📞 {rider.phone}</p>
                {rider.distanceKm !== undefined && (
                  <p>📍 {rider.distanceKm.toFixed(1)} km de distancia</p>
                )}
              </div>

              <button
                onClick={() => toggleOnline(rider)}
                className={`w-full py-2 rounded-lg text-sm font-medium ${
                  rider.isOnline
                    ? 'bg-gray-200 text-gray-700 hover:bg-gray-300'
                    : 'bg-green-600 text-white hover:bg-green-700'
                }`}
              >
                {rider.isOnline ? 'Desactivar' : 'Activar'}
              </button>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
