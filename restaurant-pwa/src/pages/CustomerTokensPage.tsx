import { useState } from 'react';
import api from '../api/client';

export default function CustomerTokensPage() {
  const [phone, setPhone] = useState('');
  const [name, setName] = useState('');
  const [result, setResult] = useState<{ token: string; publicUrl: string } | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError('');
    try {
      const { data } = await api.post<{ token: string; publicUrl: string }>('/customer-tokens', {
        phone,
        name: name || undefined,
      });
      setResult(data);
    } catch (err: any) {
      setError(err.response?.data?.error ?? 'Error al generar el enlace');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="max-w-2xl mx-auto p-4 md:p-6">
      <h1 className="text-2xl font-bold text-gray-900 mb-2">Generar enlace para cliente</h1>
      <p className="text-gray-600 mb-6">
        Crea un enlace único para que un cliente vea el catálogo y haga pedidos.
      </p>

      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg mb-4 text-sm">
          {error}
        </div>
      )}

      <form onSubmit={handleSubmit} className="bg-white rounded-xl shadow p-6 space-y-4">
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">Teléfono del cliente *</label>
          <input
            type="tel"
            required
            value={phone}
            onChange={(e) => setPhone(e.target.value)}
            className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm"
            placeholder="0991234567"
          />
        </div>
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">Nombre (opcional)</label>
          <input
            type="text"
            value={name}
            onChange={(e) => setName(e.target.value)}
            className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm"
            placeholder="Juan Pérez"
          />
        </div>
        <button
          type="submit"
          disabled={loading}
          className="bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700 font-medium text-sm disabled:opacity-50"
        >
          {loading ? 'Generando...' : 'Generar enlace'}
        </button>
      </form>

      {result && (
        <div className="mt-6 bg-green-50 border border-green-200 rounded-xl p-6">
          <h2 className="text-lg font-semibold text-green-800 mb-2">Enlace generado</h2>
          <p className="text-sm text-gray-700 mb-2">Comparte este enlace con el cliente:</p>
          <div className="bg-white border border-green-200 rounded-lg p-3 break-all text-sm text-blue-700">
            {result.publicUrl}
          </div>
          <p className="text-xs text-gray-500 mt-2">Token: {result.token}</p>
        </div>
      )}
    </div>
  );
}
