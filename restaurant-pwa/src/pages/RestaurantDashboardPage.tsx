import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import api from '../api/client';
import type { Restaurant, MenuItem } from '../types';

interface MenuItemInput {
  id?: string;
  name: string;
  description: string;
  price: string;
  imageUrl: string;
  isActive: boolean;
}

export default function RestaurantDashboardPage() {
  const { user } = useAuth();
  const navigate = useNavigate();

  const [restaurant, setRestaurant] = useState<Restaurant | null>(null);
  const [menuItems, setMenuItems] = useState<MenuItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [saving, setSaving] = useState(false);

  const [form, setForm] = useState({
    name: '',
    address: '',
    phone: '',
    menuSummary: '',
    logoUrl: '',
    paymentQrUrl: '',
  });

  const [editingItem, setEditingItem] = useState<MenuItemInput | null>(null);

  useEffect(() => {
    if (!user || user.role !== 'RestaurantAdmin') {
      navigate('/login');
      return;
    }
    loadData();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [user]);

  const loadData = async () => {
    setLoading(true);
    try {
      const [profileRes, menuRes] = await Promise.all([
        api.get<Restaurant>('/restaurant/profile'),
        api.get<MenuItem[]>('/restaurant/menu'),
      ]);
      setRestaurant(profileRes.data);
      setMenuItems(menuRes.data);
      setForm({
        name: profileRes.data.name,
        address: profileRes.data.address,
        phone: profileRes.data.phone,
        menuSummary: profileRes.data.menuSummary || '',
        logoUrl: profileRes.data.logoUrl || '',
        paymentQrUrl: '', // no viene en el DTO público
      });
    } catch {
      setError('No se pudo cargar la información del restaurante.');
    } finally {
      setLoading(false);
    }
  };

  const handleUpdateProfile = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    try {
      await api.put('/restaurant/profile', {
        name: form.name,
        address: form.address,
        phone: form.phone,
        menuSummary: form.menuSummary,
        logoUrl: form.logoUrl,
        paymentQrUrl: form.paymentQrUrl,
      });
      await loadData();
    } catch (err: any) {
      setError(err.response?.data?.error || 'Error al guardar.');
    } finally {
      setSaving(false);
    }
  };

  const startNewItem = () => {
    setEditingItem({ name: '', description: '', price: '', imageUrl: '', isActive: true });
  };

  const startEditItem = (item: MenuItem) => {
    setEditingItem({
      id: item.id,
      name: item.name,
      description: item.description || '',
      price: item.price.toString(),
      imageUrl: item.imageUrl || '',
      isActive: item.isActive,
    });
  };

  const cancelEditItem = () => setEditingItem(null);

  const saveItem = async () => {
    if (!editingItem || !editingItem.name.trim() || Number(editingItem.price) <= 0) return;
    const payload = {
      name: editingItem.name.trim(),
      description: editingItem.description.trim() || undefined,
      price: Number(editingItem.price),
      imageUrl: editingItem.imageUrl.trim() || undefined,
    };

    try {
      if (editingItem.id) {
        await api.put(`/restaurant/menu/items/${editingItem.id}`, { ...payload, isActive: editingItem.isActive });
      } else {
        await api.post('/restaurant/menu/items', payload);
      }
      setEditingItem(null);
      await loadData();
    } catch (err: any) {
      setError(err.response?.data?.error || 'Error al guardar el ítem.');
    }
  };

  const deleteItem = async (id: string) => {
    if (!confirm('¿Eliminar este ítem del menú?')) return;
    try {
      await api.delete(`/restaurant/menu/items/${id}`);
      await loadData();
    } catch (err: any) {
      setError(err.response?.data?.error || 'Error al eliminar.');
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
      <div className="max-w-4xl mx-auto space-y-6">
        {error && (
          <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg text-sm">
            {error}
          </div>
        )}

        <div className="bg-white rounded-xl shadow-lg p-6">
          <div className="flex items-center justify-between mb-4">
            <h1 className="text-2xl font-bold text-blue-700">Mi restaurante</h1>
            {restaurant?.logoUrl && (
              <img src={restaurant.logoUrl} alt="" className="h-12 w-12 object-cover rounded-lg" />
            )}
          </div>

          <form onSubmit={handleUpdateProfile} className="grid md:grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Nombre</label>
              <input
                type="text"
                value={form.name}
                onChange={(e) => setForm({ ...form, name: e.target.value })}
                className="w-full p-2 border border-gray-300 rounded-lg"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Teléfono</label>
              <input
                type="tel"
                value={form.phone}
                onChange={(e) => setForm({ ...form, phone: e.target.value })}
                className="w-full p-2 border border-gray-300 rounded-lg"
              />
            </div>
            <div className="md:col-span-2">
              <label className="block text-sm font-medium text-gray-700 mb-1">Dirección</label>
              <input
                type="text"
                value={form.address}
                onChange={(e) => setForm({ ...form, address: e.target.value })}
                className="w-full p-2 border border-gray-300 rounded-lg"
              />
            </div>
            <div className="md:col-span-2">
              <label className="block text-sm font-medium text-gray-700 mb-1">Resumen del menú</label>
              <input
                type="text"
                value={form.menuSummary}
                onChange={(e) => setForm({ ...form, menuSummary: e.target.value })}
                className="w-full p-2 border border-gray-300 rounded-lg"
                placeholder="Ej: Hamburguesas, pizzas, tacos"
              />
            </div>
            <div className="md:col-span-2">
              <label className="block text-sm font-medium text-gray-700 mb-1">URL del logo</label>
              <input
                type="url"
                value={form.logoUrl}
                onChange={(e) => setForm({ ...form, logoUrl: e.target.value })}
                className="w-full p-2 border border-gray-300 rounded-lg"
              />
            </div>
            <div className="md:col-span-2">
              <label className="block text-sm font-medium text-gray-700 mb-1">URL del QR de pago (opcional)</label>
              <input
                type="url"
                value={form.paymentQrUrl}
                onChange={(e) => setForm({ ...form, paymentQrUrl: e.target.value })}
                className="w-full p-2 border border-gray-300 rounded-lg"
              />
            </div>
            <div className="md:col-span-2">
              <button
                type="submit"
                disabled={saving}
                className="bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700 disabled:opacity-50"
              >
                {saving ? 'Guardando...' : 'Guardar cambios'}
              </button>
            </div>
          </form>
        </div>

        <div className="bg-white rounded-xl shadow-lg p-6">
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-xl font-bold text-gray-800">Menú</h2>
            <button
              onClick={startNewItem}
              className="bg-green-600 text-white px-3 py-1.5 rounded-lg hover:bg-green-700 text-sm"
            >
              + Agregar ítem
            </button>
          </div>

          {editingItem && (
            <div className="border border-blue-200 bg-blue-50 rounded-lg p-4 mb-4">
              <h3 className="font-semibold text-gray-800 mb-2">
                {editingItem.id ? 'Editar ítem' : 'Nuevo ítem'}
              </h3>
              <div className="grid md:grid-cols-2 gap-3">
                <input
                  type="text"
                  placeholder="Nombre"
                  value={editingItem.name}
                  onChange={(e) => setEditingItem({ ...editingItem, name: e.target.value })}
                  className="p-2 border border-gray-300 rounded-lg"
                />
                <input
                  type="text"
                  placeholder="Descripción"
                  value={editingItem.description}
                  onChange={(e) => setEditingItem({ ...editingItem, description: e.target.value })}
                  className="p-2 border border-gray-300 rounded-lg"
                />
                <input
                  type="number"
                  step="0.01"
                  min="0"
                  placeholder="Precio"
                  value={editingItem.price}
                  onChange={(e) => setEditingItem({ ...editingItem, price: e.target.value })}
                  className="p-2 border border-gray-300 rounded-lg"
                />
                <input
                  type="url"
                  placeholder="URL imagen"
                  value={editingItem.imageUrl}
                  onChange={(e) => setEditingItem({ ...editingItem, imageUrl: e.target.value })}
                  className="p-2 border border-gray-300 rounded-lg"
                />
              </div>
              <div className="mt-3 flex gap-2">
                <button
                  onClick={saveItem}
                  className="bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700 text-sm"
                >
                  Guardar
                </button>
                <button
                  onClick={cancelEditItem}
                  className="bg-gray-200 text-gray-700 px-4 py-2 rounded-lg hover:bg-gray-300 text-sm"
                >
                  Cancelar
                </button>
              </div>
            </div>
          )}

          <div className="divide-y divide-gray-100">
            {menuItems.length === 0 ? (
              <p className="text-gray-500 py-4">Aún no tienes ítems en el menú.</p>
            ) : (
              menuItems.map((item) => (
                <div key={item.id} className="py-4 flex items-start gap-4">
                  {item.imageUrl && (
                    <img src={item.imageUrl} alt="" className="h-16 w-16 object-cover rounded-lg" />
                  )}
                  <div className="flex-1">
                    <div className="flex items-center justify-between">
                      <h3 className="font-semibold text-gray-800">{item.name}</h3>
                      <span className="text-green-700 font-medium">${item.price.toFixed(2)}</span>
                    </div>
                    <p className="text-sm text-gray-500">{item.description}</p>
                    {!item.isActive && <span className="text-xs text-red-600">Inactivo</span>}
                  </div>
                  <div className="flex gap-2">
                    <button
                      onClick={() => startEditItem(item)}
                      className="text-sm text-blue-600 hover:underline"
                    >
                      Editar
                    </button>
                    <button
                      onClick={() => deleteItem(item.id)}
                      className="text-sm text-red-600 hover:underline"
                    >
                      Eliminar
                    </button>
                  </div>
                </div>
              ))
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
