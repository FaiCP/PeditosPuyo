import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import api from '../api/client';
import type { RestaurantClaim } from '../types';

export default function RestaurantClaimsPage() {
  const { user } = useAuth();
  const navigate = useNavigate();
  const [claims, setClaims] = useState<RestaurantClaim[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [notes, setNotes] = useState<Record<string, string>>({});
  const [processing, setProcessing] = useState<Record<string, boolean>>({});

  useEffect(() => {
    if (!user || (user.role !== 'CompanyAdmin' && user.role !== 'SuperAdmin')) {
      navigate('/login');
      return;
    }
    loadClaims();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [user]);

  const loadClaims = async () => {
    setLoading(true);
    try {
      const { data } = await api.get<RestaurantClaim[]>('/restaurant-claims/pending');
      setClaims(data);
    } catch {
      setError('No se pudo cargar las solicitudes.');
    } finally {
      setLoading(false);
    }
  };

  const handleResolve = async (id: string, action: 'approve' | 'reject') => {
    setProcessing((prev) => ({ ...prev, [id]: true }));
    try {
      await api.post(`/restaurant-claims/${id}/${action}`, { notes: notes[id] || undefined });
      await loadClaims();
    } catch (err: any) {
      setError(err.response?.data?.error || 'Error al procesar la solicitud.');
    } finally {
      setProcessing((prev) => ({ ...prev, [id]: false }));
    }
  };

  if (loading) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <p className="text-gray-500">Cargando...</p>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50 py-6 px-4">
      <div className="max-w-4xl mx-auto">
        <h1 className="text-2xl font-bold text-blue-700 mb-4">Solicitudes de reclamo</h1>

        {error && (
          <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg mb-4 text-sm">
            {error}
          </div>
        )}

        {claims.length === 0 ? (
          <div className="bg-white rounded-xl shadow p-8 text-center text-gray-500">
            No hay solicitudes pendientes.
          </div>
        ) : (
          <div className="space-y-4">
            {claims.map((claim) => (
              <div key={claim.id} className="bg-white rounded-xl shadow p-5">
                <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-4">
                  <div>
                    <h2 className="font-semibold text-gray-800">{claim.restaurantName}</h2>
                    <p className="text-sm text-gray-600">
                      Solicitante: {claim.fullName} — {claim.email} — {claim.phone}
                    </p>
                    <p className="text-xs text-gray-400 mt-1">
                      Enviado: {new Date(claim.submittedAt).toLocaleString('es-EC')}
                    </p>
                  </div>
                  <div className="flex gap-2">
                    <button
                      onClick={() => handleResolve(claim.id, 'approve')}
                      disabled={processing[claim.id]}
                      className="bg-green-600 text-white px-4 py-2 rounded-lg hover:bg-green-700 disabled:opacity-50 text-sm"
                    >
                      Aprobar
                    </button>
                    <button
                      onClick={() => handleResolve(claim.id, 'reject')}
                      disabled={processing[claim.id]}
                      className="bg-red-600 text-white px-4 py-2 rounded-lg hover:bg-red-700 disabled:opacity-50 text-sm"
                    >
                      Rechazar
                    </button>
                  </div>
                </div>
                <div className="mt-3">
                  <label className="block text-xs font-medium text-gray-600 mb-1">Notas (opcional)</label>
                  <input
                    type="text"
                    value={notes[claim.id] || ''}
                    onChange={(e) => setNotes({ ...notes, [claim.id]: e.target.value })}
                    className="w-full p-2 border border-gray-300 rounded-lg text-sm"
                    placeholder="Motivo del rechazo u observaciones"
                  />
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
