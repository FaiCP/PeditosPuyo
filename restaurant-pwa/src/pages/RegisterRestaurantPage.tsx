import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

interface MenuItemInput {
  name: string;
  description: string;
  price: string;
  imageUrl: string;
}

export default function RegisterRestaurantPage() {
  const { register } = useAuth();
  const navigate = useNavigate();

  const [fullName, setFullName] = useState('');
  const [email, setEmail] = useState('');
  const [phone, setPhone] = useState('');
  const [password, setPassword] = useState('');
  const [restaurantName, setRestaurantName] = useState('');
  const [restaurantAddress, setRestaurantAddress] = useState('');
  const [restaurantPhone, setRestaurantPhone] = useState('');
  const [logoUrl, setLogoUrl] = useState('');
  const [menuItems, setMenuItems] = useState<MenuItemInput[]>([]);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const addMenuItem = () => {
    setMenuItems([...menuItems, { name: '', description: '', price: '', imageUrl: '' }]);
  };

  const removeMenuItem = (index: number) => {
    setMenuItems(menuItems.filter((_, i) => i !== index));
  };

  const updateMenuItem = (index: number, field: keyof MenuItemInput, value: string) => {
    const next = [...menuItems];
    next[index][field] = value;
    setMenuItems(next);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

    if (!restaurantName.trim() || !restaurantAddress.trim() || !restaurantPhone.trim()) {
      setError('Nombre, dirección y teléfono del restaurante son obligatorios.');
      return;
    }

    const validMenuItems = menuItems
      .filter((i) => i.name.trim() && Number(i.price) > 0)
      .map((i) => ({
        name: i.name.trim(),
        description: i.description.trim() || undefined,
        price: Number(i.price),
        imageUrl: i.imageUrl.trim() || undefined,
      }));

    setLoading(true);
    try {
      await register({
        fullName,
        email,
        phone,
        password,
        role: 'RestaurantAdmin',
        restaurantName,
        restaurantAddress,
        restaurantPhone,
        logoUrl: logoUrl.trim() || undefined,
        initialMenuItems: validMenuItems.length > 0 ? validMenuItems : undefined,
      });
      navigate('/restaurant/dashboard');
    } catch (err: any) {
      setError(err.response?.data?.error || 'No se pudo crear la cuenta. Intenta de nuevo.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-gray-50 py-8 px-4">
      <div className="max-w-2xl mx-auto bg-white rounded-xl shadow-lg p-6 md:p-8">
        <h1 className="text-2xl font-bold text-blue-700 mb-2">Registrar restaurante</h1>
        <p className="text-gray-600 mb-6">Crea tu cuenta y da de alta tu restaurante en la plataforma.</p>

        {error && (
          <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg mb-4 text-sm">
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-6">
          <section>
            <h2 className="text-lg font-semibold text-gray-800 mb-3">Datos del administrador</h2>
            <div className="grid md:grid-cols-2 gap-4">
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
            </div>
          </section>

          <section>
            <h2 className="text-lg font-semibold text-gray-800 mb-3">Datos del restaurante</h2>
            <div className="grid md:grid-cols-2 gap-4">
              <div className="md:col-span-2">
                <label className="block text-sm font-medium text-gray-700 mb-1">Nombre del restaurante *</label>
                <input
                  type="text"
                  value={restaurantName}
                  onChange={(e) => setRestaurantName(e.target.value)}
                  className="w-full p-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500"
                  required
                />
              </div>
              <div className="md:col-span-2">
                <label className="block text-sm font-medium text-gray-700 mb-1">Dirección *</label>
                <input
                  type="text"
                  value={restaurantAddress}
                  onChange={(e) => setRestaurantAddress(e.target.value)}
                  className="w-full p-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500"
                  required
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Teléfono del restaurante *</label>
                <input
                  type="tel"
                  value={restaurantPhone}
                  onChange={(e) => setRestaurantPhone(e.target.value)}
                  className="w-full p-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500"
                  required
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">URL del logo (opcional)</label>
                <input
                  type="url"
                  value={logoUrl}
                  onChange={(e) => setLogoUrl(e.target.value)}
                  className="w-full p-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500"
                  placeholder="https://..."
                />
              </div>
            </div>
          </section>

          <section>
            <div className="flex items-center justify-between mb-3">
              <h2 className="text-lg font-semibold text-gray-800">Menú inicial (opcional)</h2>
              <button
                type="button"
                onClick={addMenuItem}
                className="text-sm px-3 py-1.5 bg-green-600 text-white rounded-lg hover:bg-green-700"
              >
                + Agregar ítem
              </button>
            </div>
            <div className="space-y-3">
              {menuItems.map((item, index) => (
                <div key={index} className="border border-gray-200 rounded-lg p-4 bg-gray-50">
                  <div className="grid md:grid-cols-2 gap-3">
                    <input
                      type="text"
                      placeholder="Nombre *"
                      value={item.name}
                      onChange={(e) => updateMenuItem(index, 'name', e.target.value)}
                      className="p-2 border border-gray-300 rounded-lg"
                    />
                    <input
                      type="text"
                      placeholder="Descripción"
                      value={item.description}
                      onChange={(e) => updateMenuItem(index, 'description', e.target.value)}
                      className="p-2 border border-gray-300 rounded-lg"
                    />
                    <input
                      type="number"
                      step="0.01"
                      min="0"
                      placeholder="Precio *"
                      value={item.price}
                      onChange={(e) => updateMenuItem(index, 'price', e.target.value)}
                      className="p-2 border border-gray-300 rounded-lg"
                    />
                    <input
                      type="url"
                      placeholder="URL imagen (opcional)"
                      value={item.imageUrl}
                      onChange={(e) => updateMenuItem(index, 'imageUrl', e.target.value)}
                      className="p-2 border border-gray-300 rounded-lg"
                    />
                  </div>
                  <button
                    type="button"
                    onClick={() => removeMenuItem(index)}
                    className="mt-2 text-sm text-red-600 hover:underline"
                  >
                    Eliminar ítem
                  </button>
                </div>
              ))}
            </div>
          </section>

          <button
            type="submit"
            disabled={loading}
            className="w-full bg-blue-600 text-white p-3 rounded-lg hover:bg-blue-700 disabled:opacity-50 font-medium"
          >
            {loading ? 'Creando...' : 'Crear cuenta y restaurante'}
          </button>
        </form>

        <div className="mt-6 text-center text-sm text-gray-500 space-y-2">
          <p>
            ¿Tu restaurante ya está en la plataforma?{' '}
            <Link to="/claim-restaurant" className="text-blue-600 hover:underline">
              Reclamar restaurante
            </Link>
          </p>
          <p>
            ¿Ya tienes cuenta?{' '}
            <Link to="/login" className="text-blue-600 hover:underline">
              Iniciar sesión
            </Link>
          </p>
        </div>
      </div>
    </div>
  );
}
