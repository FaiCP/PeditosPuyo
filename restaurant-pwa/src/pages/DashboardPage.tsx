import { useEffect, useState, useCallback } from 'react';
import { Link } from 'react-router-dom';
import api from '../api/client';
import { useAuth } from '../context/AuthContext';
import { useSignalR } from '../hooks/useSignalR';
import { useSoundAlert } from '../hooks/useSoundAlert';
import type { DeliveryRequest } from '../types';

const STATUS_LABELS: Record<string, string> = {
  Pending: 'Pendiente',
  Assigned: 'Asignado',
  Accepted: 'Aceptado',
  InTransit: 'En camino',
  Delivered: 'Entregado',
  Cancelled: 'Cancelado',
};

const STATUS_COLORS: Record<string, string> = {
  Pending: 'bg-yellow-100 text-yellow-800 border-yellow-200',
  Assigned: 'bg-blue-100 text-blue-800 border-blue-200',
  Accepted: 'bg-green-100 text-green-800 border-green-200',
  InTransit: 'bg-purple-100 text-purple-800 border-purple-200',
  Delivered: 'bg-emerald-100 text-emerald-800 border-emerald-200',
  Cancelled: 'bg-red-100 text-red-800 border-red-200',
};

export default function DashboardPage() {
  const { user } = useAuth();
  const [requests, setRequests] = useState<DeliveryRequest[]>([]);
  const [loading, setLoading] = useState(true);
  const [filter, setFilter] = useState<'all' | 'active' | 'delivered'>('active');
  const { playAlert } = useSoundAlert();

  const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5001/api';
  const HUB_URL = API_URL.replace('/api', '');

  const { on } = useSignalR(
    HUB_URL,
    user?.tenantId || '',
    'company'
  );

  const fetchRequests = useCallback(async () => {
    try {
      const { data } = await api.get<DeliveryRequest[]>('/delivery-requests');
      setRequests(data);
    } catch (error) {
      console.error('Error fetching requests:', error);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchRequests();
  }, [fetchRequests]);

  useEffect(() => {
    on('newDeliveryRequest', (data: unknown) => {
      playAlert();
      const newData = data as { requestId: string; restaurantName: string; address: string };
      setRequests((prev) => [
        {
          id: newData.requestId,
          restaurantId: '',
          restaurantName: newData.restaurantName,
          status: 'Pending',
          deliveryAddress: newData.address,
          lat: 0,
          lng: 0,
          createdAt: new Date().toISOString(),
        },
        ...prev,
      ]);
    });

    on('requestStatusChanged', (data: unknown) => {
      const update = data as { requestId: string; status: string };
      setRequests((prev) =>
        prev.map((r) => (r.id === update.requestId ? { ...r, status: update.status } : r))
      );
    });
  }, [on, playAlert]);

  const filtered = requests.filter((r) => {
    if (filter === 'active') return r.status !== 'Delivered' && r.status !== 'Cancelled';
    if (filter === 'delivered') return r.status === 'Delivered';
    return true;
  });

  const stats = {
    total: requests.length,
    pending: requests.filter((r) => r.status === 'Pending').length,
    active: requests.filter((r) => ['Assigned', 'Accepted', 'InTransit'].includes(r.status)).length,
    delivered: requests.filter((r) => r.status === 'Delivered').length,
  };

  return (
    <div className="max-w-6xl mx-auto p-4 md:p-6">
      <header className="flex flex-col md:flex-row md:justify-between md:items-center mb-6 gap-4">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Dashboard Restaurante</h1>
          <p className="text-gray-600">Bienvenido, {user?.fullName}</p>
        </div>
        <div className="flex gap-3">
          <Link
            to="/new-delivery"
            className="bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700 font-medium text-sm"
          >
            + Nuevo Delivery
          </Link>
          <button
            onClick={fetchRequests}
            className="bg-gray-200 px-4 py-2 rounded-lg hover:bg-gray-300 font-medium text-sm"
          >
            ↻ Actualizar
          </button>
        </div>
      </header>

      <div className="grid grid-cols-2 md:grid-cols-4 gap-3 mb-6">
        <StatCard label="Total" value={stats.total} color="bg-gray-100" />
        <StatCard label="Pendientes" value={stats.pending} color="bg-yellow-100" />
        <StatCard label="Activos" value={stats.active} color="bg-blue-100" />
        <StatCard label="Entregados" value={stats.delivered} color="bg-green-100" />
      </div>

      <div className="flex gap-2 mb-6">
        <FilterButton active={filter === 'active'} onClick={() => setFilter('active')}>
          Activos
        </FilterButton>
        <FilterButton active={filter === 'all'} onClick={() => setFilter('all')}>
          Todos
        </FilterButton>
        <FilterButton active={filter === 'delivered'} onClick={() => setFilter('delivered')}>
          Entregados
        </FilterButton>
      </div>

      {loading ? (
        <div className="text-center py-12">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto"></div>
          <p className="text-gray-500 mt-4">Cargando deliveries...</p>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {filtered.map((r) => (
            <DeliveryCard key={r.id} request={r} />
          ))}
        </div>
      )}

      {!loading && filtered.length === 0 && (
        <div className="text-center py-12">
          <p className="text-gray-500 text-lg">No hay deliveries {filter === 'active' ? 'activos' : ''}.</p>
          <Link to="/new-delivery" className="text-blue-600 hover:text-blue-800 mt-2 inline-block">
            Crear uno nuevo →
          </Link>
        </div>
      )}
    </div>
  );
}

function StatCard({ label, value, color }: { label: string; value: number; color: string }) {
  return (
    <div className={`${color} rounded-lg p-4`}>
      <p className="text-sm text-gray-600">{label}</p>
      <p className="text-2xl font-bold text-gray-900">{value}</p>
    </div>
  );
}

function FilterButton({
  active,
  onClick,
  children,
}: {
  active: boolean;
  onClick: () => void;
  children: React.ReactNode;
}) {
  return (
    <button
      onClick={onClick}
      className={`px-4 py-2 rounded-lg text-sm font-medium ${
        active ? 'bg-blue-600 text-white' : 'bg-gray-200 text-gray-700 hover:bg-gray-300'
      }`}
    >
      {children}
    </button>
  );
}

function DeliveryCard({ request }: { request: DeliveryRequest }) {
  const timeAgo = (date: string) => {
    const diff = Date.now() - new Date(date).getTime();
    const mins = Math.floor(diff / 60000);
    if (mins < 1) return 'Ahora';
    if (mins < 60) return `Hace ${mins}min`;
    const hours = Math.floor(mins / 60);
    return `Hace ${hours}h`;
  };

  return (
    <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-4 hover:shadow-md transition">
      <div className="flex justify-between items-start mb-3">
        <h3 className="font-semibold text-gray-900">{request.restaurantName}</h3>
        <span
          className={`px-2 py-1 rounded-full text-xs font-medium border ${
            STATUS_COLORS[request.status] || 'bg-gray-100 text-gray-800 border-gray-200'
          }`}
        >
          {STATUS_LABELS[request.status] || request.status}
        </span>
      </div>

      <div className="space-y-1 text-sm">
        <p className="text-gray-600 flex items-start gap-2">
          <span className="text-gray-400">📍</span>
          {request.deliveryAddress}
        </p>
        {request.notes && (
          <p className="text-gray-500 flex items-start gap-2">
            <span className="text-gray-400">📝</span>
            {request.notes}
          </p>
        )}
      </div>

      <div className="mt-3 pt-3 border-t border-gray-100 flex justify-between items-center">
        <span className="text-xs text-gray-400">{timeAgo(request.createdAt)}</span>
        {request.assignedAt && (
          <span className="text-xs text-blue-600">
            Rider asignado {timeAgo(request.assignedAt)}
          </span>
        )}
      </div>
    </div>
  );
}
