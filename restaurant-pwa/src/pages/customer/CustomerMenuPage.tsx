import { useEffect, useMemo, useState, type ReactNode } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { createCustomerApi } from '../../api/customerApi';
import type { PublicRestaurantDetail, CreateOrderRequest, OrderTracking } from '../../types/customer';
import { money } from '../../types/customer';
import LocationField from './LocationField';
import { BrokenLink, useBrokenLink } from './CustomerCatalogPage';
import type { Coords } from '../../utils/coords';
import { savedProfile, rememberProfile } from './profile';

export default function CustomerMenuPage() {
  const { token = '', restaurantId = '' } = useParams();
  const navigate = useNavigate();
  const api = useMemo(() => createCustomerApi(token), [token]);

  const [rest, setRest] = useState<PublicRestaurantDetail | null>(null);
  const [broken, onError] = useBrokenLink();
  const [cart, setCart] = useState<Record<string, number>>({});
  const [sheet, setSheet] = useState(false);

  useEffect(() => {
    api.get<PublicRestaurantDetail>(`/restaurants/${restaurantId}`).then(({ data }) => setRest(data)).catch(onError);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [api, restaurantId]);

  const count = Object.values(cart).reduce((a, b) => a + b, 0);
  const total = useMemo(() => {
    if (!rest) return 0;
    return Object.entries(cart).reduce((sum, [id, qty]) => {
      const m = rest.menu.find((x) => x.id === id);
      return sum + (m ? m.price * qty : 0);
    }, 0);
  }, [cart, rest]);

  if (broken) return <BrokenLink />;
  if (!rest) return <div className="cust-skeleton h-40 mt-6" />;

  const addItem = (id: string, delta: number) =>
    setCart((c) => {
      const q = Math.max(0, (c[id] ?? 0) + delta);
      const next = { ...c, [id]: q };
      if (q === 0) delete next[id];
      return next;
    });

  return (
    <div>
      <Link to={`/p/${token}`} className="text-sm font-semibold" style={{ color: 'var(--cream-dim)' }}>
        ← Catálogo
      </Link>
      <h1 className="cust-display text-3xl mt-3 mb-1">{rest.name}</h1>
      <p className="text-sm mb-1" style={{ color: 'var(--cream-dim)' }}>
        📍 {rest.address}
      </p>
      {rest.phone && (
        <a href={`tel:${rest.phone}`} className="text-sm font-semibold" style={{ color: 'var(--leaf)' }}>
          📞 {rest.phone}
        </a>
      )}

      <div className="mt-6 grid gap-3 cust-stagger">
        {rest.menu.length === 0 && (
          <p className="py-8 text-center" style={{ color: 'var(--cream-dim)' }}>
            Este restaurante aún no publica su menú por aquí.
          </p>
        )}
        {rest.menu.map((m) => {
          const q = cart[m.id] ?? 0;
          return (
            <div key={m.id} className="cust-card p-4 flex items-center justify-between gap-3">
              <div className="min-w-0">
                <b className="block truncate">{m.name}</b>
                {m.description && (
                  <span className="text-xs block truncate" style={{ color: 'var(--cream-dim)' }}>
                    {m.description}
                  </span>
                )}
                <span className="cust-price text-sm">{money(m.price)}</span>
              </div>
              {q === 0 ? (
                <button className="cust-btn cust-btn-primary py-2 px-4" onClick={() => addItem(m.id, 1)}>
                  + Agregar
                </button>
              ) : (
                <span className="cust-qty">
                  <button onClick={() => addItem(m.id, -1)}>−</button>
                  <b>{q}</b>
                  <button onClick={() => addItem(m.id, 1)}>+</button>
                </span>
              )}
            </div>
          );
        })}
      </div>

      {count > 0 && !sheet && (
        <div className="cust-cartbar">
          <button className="cust-cartbar-inner w-full" onClick={() => setSheet(true)}>
            <span>
              {count} · {money(total)}
            </span>
            <span>Pedir ahora →</span>
          </button>
        </div>
      )}

      {sheet && (
        <CheckoutSheet
          title="Confirmar pedido"
          onClose={() => setSheet(false)}
          allowQr={!!rest.paymentQrUrl}
          summary={
            <div className="mb-4">
              {Object.entries(cart).map(([id, qty]) => {
                const m = rest.menu.find((x) => x.id === id)!;
                return (
                  <div key={id} className="flex justify-between text-sm py-1">
                    <span>
                      {qty}× {m.name}
                    </span>
                    <span className="cust-price">{money(m.price * qty)}</span>
                  </div>
                );
              })}
              <div className="flex justify-between text-xs pt-2" style={{ color: 'var(--cream-dim)' }}>
                <span>Subtotal (sin tarifa de delivery)</span>
                <span>{money(total)}</span>
              </div>
            </div>
          }
          onSubmit={async (payload) => {
            const req: CreateOrderRequest = {
              ...payload,
              type: 'restaurant',
              restaurantId: rest.id,
              items: Object.entries(cart).map(([id, qty]) => {
                const m = rest.menu.find((x) => x.id === id)!;
                return { menuItemId: m.id, name: m.name, quantity: qty, unitPrice: m.price };
              }),
            };
            const { data } = await api.post<OrderTracking>('/orders', req);
            rememberProfile(payload.customerName, payload.customerPhone);
            navigate(`/p/${token}/order/${data.id}`);
          }}
        />
      )}
    </div>
  );
}

// ---- Hoja de checkout compartida (menú y pedido personalizado) ----
export interface CheckoutValues {
  customerName: string;
  customerPhone: string;
  destinationAddress: string;
  destinationLat: number;
  destinationLng: number;
  destinationLinkRaw?: string;
  paymentMethod: 'cash' | 'restaurantQr';
}

export function CheckoutSheet({
  title,
  summary,
  allowQr,
  onClose,
  onSubmit,
}: {
  title: string;
  summary: ReactNode;
  allowQr: boolean;
  onClose: () => void;
  onSubmit: (v: CheckoutValues) => Promise<void>;
}) {
  const [dest, setDest] = useState<Coords | null>(null);
  const [destLink, setDestLink] = useState('');
  const [address, setAddress] = useState('');
  const [name, setName] = useState(savedProfile().name);
  const [phone, setPhone] = useState(savedProfile().phone);
  const [qr, setQr] = useState(false);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  const submit = async () => {
    setError('');
    if (!name.trim() || !phone.trim()) return setError('Pon tu nombre y teléfono.');
    if (!address.trim()) return setError('Escribe la dirección de entrega.');
    if (!dest) return setError('Falta la ubicación: pega el link de Google Maps o usa tu ubicación.');
    setBusy(true);
    try {
      await onSubmit({
        customerName: name.trim(),
        customerPhone: phone.trim(),
        destinationAddress: address.trim(),
        destinationLat: dest.lat,
        destinationLng: dest.lng,
        destinationLinkRaw: destLink || undefined,
        paymentMethod: qr && allowQr ? 'restaurantQr' : 'cash',
      });
    } catch (e) {
      const resp = (e as { response?: { data?: { error?: string } } }).response;
      setError(resp?.data?.error || 'No se pudo enviar el pedido. Inténtalo de nuevo.');
      setBusy(false);
    }
  };

  return (
    <>
      <div className="cust-sheet-backdrop" onClick={onClose} />
      <div className="cust-sheet">
        <div className="flex items-center justify-between mb-4">
          <h2 className="cust-display text-xl">{title}</h2>
          <button onClick={onClose} className="text-xl px-2" style={{ color: 'var(--cream-dim)' }} aria-label="Cerrar">
            ✕
          </button>
        </div>

        {summary}

        <div className="grid gap-4">
          <div>
            <label className="cust-label">Dirección de entrega *</label>
            <input
              className="cust-input"
              value={address}
              onChange={(e) => setAddress(e.target.value)}
              placeholder="Calle, número, referencia…"
            />
          </div>

          <LocationField
            label="Ubicación (link de Google Maps) *"
            value={dest}
            linkValue={destLink}
            onChange={(c, link) => {
              setDest(c);
              setDestLink(link);
            }}
            hint="Google Maps → buscar tu punto → Compartir → Copiar link. También sirve «lat, lng»."
          />

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="cust-label">Tu nombre *</label>
              <input className="cust-input" value={name} onChange={(e) => setName(e.target.value)} />
            </div>
            <div>
              <label className="cust-label">Tu teléfono *</label>
              <input className="cust-input" value={phone} onChange={(e) => setPhone(e.target.value)} inputMode="tel" />
            </div>
          </div>

          {allowQr && (
            <label className="flex items-center gap-3 text-sm cursor-pointer">
              <input type="checkbox" checked={qr} onChange={(e) => setQr(e.target.checked)} className="w-5 h-5 accent-[var(--tang)]" />
              Pagar por QR del restaurante (si no: efectivo al recibir)
            </label>
          )}

          {error && <p className="text-sm" style={{ color: 'var(--danger)' }}>{error}</p>}

          <button className="cust-btn cust-btn-primary" onClick={submit} disabled={busy}>
            {busy ? 'Enviando…' : 'Enviar pedido 🚀'}
          </button>
          <p className="text-xs text-center" style={{ color: 'var(--cream-dim)' }}>
            Se agrega la tarifa de delivery. Pagas al recibir.
          </p>
        </div>
      </div>
    </>
  );
}
