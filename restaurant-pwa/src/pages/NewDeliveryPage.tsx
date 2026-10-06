import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import api from '../api/client';
import type { Restaurant } from '../types';

// Extrae [lat, lng] de un link de Google Maps en varios formatos, o de "lat, lng" directo
function parseCoords(input: string): { lat: number; lng: number } | null {
  const s = input.trim();
  if (!s) return null;

  // Formato directo: -1.0512, -78.4699
  const direct = s.match(/^\s*(-?\d+(?:[.,]\d+)?)\s*[, ]\s*(-?\d+(?:[.,]\d+)?)\s*$/);
  if (direct) {
    return { lat: toNum(direct[1]), lng: toNum(direct[2]) };
  }

  // @lat,lng (vista de mapa)
  const at = s.match(/@(-?\d+(?:\.\d+)?),(-?\d+(?:\.\d+)?)/);
  if (at) return { lat: +at[1], lng: +at[2] };

  // q=lat,lng / query=lat,lng / lat=..&lng=.. / /data=...!3dlat!4dlng
  const q = s.match(/[?&](?:q|query)=(-?\d+(?:\.\d+)?),(-?\d+(?:\.\d+)?)/);
  if (q) return { lat: +q[1], lng: +q[2] };

  const ll = s.match(/[?&]lat=(-?\d+(?:\.\d+)?)[&].*?lng=(-?\d+(?:\.\d+)?)/i);
  if (ll) return { lat: +ll[1], lng: +ll[2] };

  const data = s.match(/!3d(-?\d+(?:\.\d+)?)!4d(-?\d+(?:\.\d+)?)/);
  if (data) return { lat: +data[1], lng: +data[2] };

  // place/.../-1.0512,-78.4699
  const place = s.match(/\/(-?\d+(?:\.\d+)?),(-?\d+(?:\.\d+)?)\/data/);
  if (place) return { lat: +place[1], lng: +place[2] };

  return null;
}

function toNum(v: string): number {
  return parseFloat(v.replace(',', '.'));
}

function inBounds(lat: number, lng: number): boolean {
  return lat >= -90 && lat <= 90 && lng >= -180 && lng <= 180;
}

export default function NewDeliveryPage() {
  const navigate = useNavigate();
  const [restaurants, setRestaurants] = useState<Restaurant[]>([]);
  const [restaurantId, setRestaurantId] = useState('');
  const [address, setAddress] = useState('');
  const [mapLink, setMapLink] = useState('');
  const [lat, setLat] = useState('');
  const [lng, setLng] = useState('');
  const [notes, setNotes] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [linkMsg, setLinkMsg] = useState('');
  const [locationLoading, setLocationLoading] = useState(false);

  useEffect(() => {
    api
      .get<Restaurant[]>('/restaurants')
      .then(({ data }) => setRestaurants(data))
      .catch(() => setError('No se pudieron cargar los restaurantes.'));
  }, []);

  const applyLink = async () => {
    let target = mapLink;
    const isShort = /^https?:\/\/(maps\.app\.goo\.gl|goo\.gl|g\.co)\//i.test(mapLink);

    if (isShort) {
      setLinkMsg('⏳ Abriendo el link corto de Google Maps...');
      try {
        const { data } = await api.get<{ finalUrl: string }>('/resolve-link', { params: { url: mapLink } });
        target = data.finalUrl;
      } catch {
        setLinkMsg('⚠️ No pude abrir el link corto. Abre el link en el navegador y pega la URL larga, o las coordenadas.');
        return;
      }
    }

    const coords = parseCoords(target);
    if (!coords) {
      setLinkMsg('⚠️ No encontré coordenadas en ese link. Pega el link completo de "Compartir → Copiar link" de Google Maps, o escribe "lat, lng".');
      return;
    }
    if (!inBounds(coords.lat, coords.lng)) {
      setLinkMsg('⚠️ Coordenadas fuera de rango. Revisa el link.');
      return;
    }
    setLat(coords.lat.toFixed(6));
    setLng(coords.lng.toFixed(6));
    setLinkMsg(`✅ Ubicación detectada: ${coords.lat.toFixed(5)}, ${coords.lng.toFixed(5)}`);
  };

  const getLocation = () => {
    setLocationLoading(true);
    setError('');
    if (!navigator.geolocation) {
      setLinkMsg('Geolocalización no soportada en este navegador');
      setLocationLoading(false);
      return;
    }
    navigator.geolocation.getCurrentPosition(
      (position) => {
        setLat(position.coords.latitude.toFixed(6));
        setLng(position.coords.longitude.toFixed(6));
        setLinkMsg(`✅ Usando tu ubicación actual`);
        setLocationLoading(false);
      },
      (err) => {
        setLinkMsg('No se pudo obtener la ubicación: ' + err.message);
        setLocationLoading(false);
      },
      { enableHighAccuracy: true, timeout: 10000 }
    );
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

    if (!restaurantId) {
      setError('Selecciona un restaurante.');
      return;
    }
    if (!address.trim()) {
      setError('Pon la dirección de entrega.');
      return;
    }
    const latN = parseFloat(lat);
    const lngN = parseFloat(lng);
    if (isNaN(latN) || isNaN(lngN)) {
      setError('Falta la ubicación. Pega un link de Google Maps o usa tu ubicación actual.');
      return;
    }
    if (!inBounds(latN, lngN)) {
      setError('Coordenadas inválidas.');
      return;
    }

    setLoading(true);
    try {
      await api.post('/delivery-requests', {
        restaurantId,
        deliveryAddress: address,
        lat: latN,
        lng: lngN,
        notes: notes || undefined,
      });
      navigate('/dashboard');
    } catch (err: any) {
      const msg = err.response?.data?.error;
      setError(msg || 'Error al crear el delivery. Inténtalo de nuevo.');
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
          <label className="block text-sm font-medium text-gray-700 mb-2">Restaurante *</label>
          <select
            value={restaurantId}
            onChange={(e) => setRestaurantId(e.target.value)}
            className="w-full p-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent bg-white"
            required
          >
            <option value="" disabled>
              {restaurants.length === 0 ? 'Cargando restaurantes...' : 'Selecciona un restaurante'}
            </option>
            {restaurants.map((r) => (
              <option key={r.id} value={r.id}>
                {r.name}
              </option>
            ))}
          </select>
        </div>

        <div className="mb-5">
          <label className="block text-sm font-medium text-gray-700 mb-2">Dirección de entrega *</label>
          <input
            type="text"
            value={address}
            onChange={(e) => setAddress(e.target.value)}
            className="w-full p-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            placeholder="Ej: Av. Principal 123, Puyo"
            required
          />
        </div>

        <div className="mb-3">
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Ubicación en mapa (link de Google Maps)
          </label>
          <div className="flex gap-2">
            <input
              type="url"
              value={mapLink}
              onChange={(e) => {
                setMapLink(e.target.value);
                setLinkMsg('');
              }}
              className="flex-1 p-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent text-sm"
              placeholder="https://maps.app.goo.gl/... o pega las coordenadas"
            />
            <button
              type="button"
              onClick={applyLink}
              className="bg-gray-800 text-white px-4 rounded-lg hover:bg-gray-900 font-medium text-sm whitespace-nowrap"
            >
              Detectar
            </button>
          </div>
          <p className="text-xs text-gray-500 mt-1">
            En Google Maps: busca el punto → Compartir → Copiar link. También sirve escribir las coordenadas
            directo, ej: <code className="bg-gray-100 px-1 rounded">-1.0512, -78.4699</code>
          </p>
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
              <>📍 O usar mi ubicación actual</>
            )}
          </button>
        </div>

        {linkMsg && (
          <div className="text-sm mb-4 px-3 py-2 rounded-lg bg-gray-50 text-gray-700">{linkMsg}</div>
        )}

        {(lat || lng) && (
          <div className="grid grid-cols-2 gap-4 mb-5">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">Latitud</label>
              <input
                type="number"
                step="any"
                value={lat}
                onChange={(e) => setLat(e.target.value)}
                className="w-full p-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">Longitud</label>
              <input
                type="number"
                step="any"
                value={lng}
                onChange={(e) => setLng(e.target.value)}
                className="w-full p-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              />
            </div>
          </div>
        )}

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
