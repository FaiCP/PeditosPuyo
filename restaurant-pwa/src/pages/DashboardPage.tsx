import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import api from '../api/client';
import { useAuth } from '../context/AuthContext';
import type { DeliveryRequest } from '../types';

export default function DashboardPage() {
  const { user } = useAuth();
  const [requests, setRequests] = useState<DeliveryRequest[]>([]);

  useEffect(() => {
    fetchRequests();
  }, []);

  const fetchRequests = () => {
    api.get<DeliveryRequest[]>('/delivery-requests').then(({ data }) => setRequests(data));
  };

  const statusLabels: Record<string, string> = {
    Pending: 'Pendiente',
    Assigned: 'Asignado',
    Accepted: 'Aceptado',
    InTransit: 'En camino',
    Delivered: 'Entregado',
    Cancelled: 'Cancelado',
  };

  const statusColors: Record<string, string> = {
    Pending: 'bg-yellow-100 text-yellow-800',
    Assigned: 'bg-blue-100 text-blue-800',
    Accepted: 'bg-green-100 text-green-800',
    InTransit: 'bg-purple-100 text-purple-800',
    Delivered: 'bg-green-100 text-green-800',
    Cancelled: 'bg-red-100 text-red-800',
  };

  return (
    <div className="max-w-6xl mx-auto p-6">
      <header className="flex justify-between items-center mb-8">
        <div>
          <h1 className="text-2xl font-bold">Dashboard Restaurante</h1>
          <p className="text-gray-600">Bienvenido, {user?.fullName}</p>
        </div>
        <div className="flex gap-4">
          <Link
            to="/new-delivery"
            className="bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700"
          >
            Nuevo Delivery
          </Link>
          <button
            onClick={fetchRequests}
            className="bg-gray-200 px-4 py-2 rounded-lg hover:bg-gray-300"
          >
            Actualizar
          </button>
        </div>
      </header>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        {requests.map((r) => (
          <div key={r.id} className="bg-white rounded-lg shadow-md p-4">
            <div className="flex justify-between items-start mb-2">
              <h3 className="font-semibold">{r.restaurantName}</h3>
              <span className={`px-2 py-1 rounded-full text-xs ${statusColors[r.status] || 'bg-gray-100'}`}>
                {statusLabels[r.status] || r.status}
              </span>
            </div>
            <p className="text-sm text-gray-600">{r.deliveryAddress}</p>
            {r.notes && <p className="text-sm text-gray-500 mt-1">{r.notes}</p>}
            <p className="text-xs text-gray-400 mt-2">
              {new Date(r.createdAt).toLocaleString()}
            </p>
          </div>
        ))}
      </div>

      {requests.length === 0 && (
        <p className="text-center text-gray-500 mt-12">No hay deliveries todavía.</p>
      )}
    </div>
  );
}
