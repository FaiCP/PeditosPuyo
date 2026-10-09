import { useMemo, useState } from 'react';
import { Link, useNavigate, useParams, useSearchParams } from 'react-router-dom';
import { createCustomerApi } from '../../api/customerApi';
import type { CreateOrderRequest, OrderTracking } from '../../types/customer';
import { CheckoutSheet } from './CustomerMenuPage';
import LocationField from './LocationField';
import type { Coords } from '../../utils/coords';

interface Articulo {
  name: string;
  quantity: number;
  price: string;
}

export default function CustomerNewOrderPage() {
  const { token = '' } = useParams();
  const navigate = useNavigate();
  const api = useMemo(() => createCustomerApi(token), [token]);
  const [params] = useSearchParams();
  const tipo = params.get('tipo') === 'encargo' ? 'encargo' : 'compra';

  const [origin, setOrigin] = useState<Coords | null>(null);
  const [originLink, setOriginLink] = useState('');
  const [originName, setOriginName] = useState('');
  const [description, setDescription] = useState('');
  const [articulos, setArticulos] = useState<Articulo[]>([{ name: '', quantity: 1, price: '' }]);
  const [sheet, setSheet] = useState(false);

  const setArt = (i: number, patch: Partial<Articulo>) =>
    setArticulos((arr) => arr.map((a, j) => (j === i ? { ...a, ...patch } : a)));

  const canSend =
    tipo === 'encargo'
      ? origin !== null && description.trim().length > 0
      : origin !== null && articulos.some((a) => a.name.trim());

  return (
    <div>
      <Link to={`/p/${token}`} className="text-sm font-semibold" style={{ color: 'var(--cream-dim)' }}>
        ← Catálogo
      </Link>

      <div className="flex gap-2 mt-3 mb-4">
        {(['compra', 'encargo'] as const).map((t) => (
          <Link
            key={t}
            to={`/p/${token}/nuevo?tipo=${t}`}
            className="px-4 py-2 rounded-full text-sm font-bold border"
            style={
              t === tipo
                ? { background: 'var(--tang)', color: '#2a0d00', borderColor: 'var(--tang)' }
                : { borderColor: 'var(--card-edge)', color: 'var(--cream-dim)' }
            }
          >
            {t === 'compra' ? '🛒 Compra' : '📦 Encargo'}
          </Link>
        ))}
      </div>

      <h1 className="cust-display text-3xl mb-1">
        {tipo === 'compra' ? 'Que te compren' : 'Que lleven o traigan'}
      </h1>
      <p className="text-sm mb-5" style={{ color: 'var(--cream-dim)' }}>
        {tipo === 'compra'
          ? 'Lista los artículos con un precio estimado; el rider compra y me paga al recibir.'
          : 'Describe el paquete, de dónde recogerlo y a dónde llevarlo.'}
      </p>

      <div className="grid gap-5">
        <div>
          <label className="cust-label">Lugar de origen *</label>
          <input
            className="cust-input mb-2"
            placeholder="Nombre del lugar (ej: Farmacia San Pablo, mercado…)"
            value={originName}
            onChange={(e) => setOriginName(e.target.value)}
          />
          <LocationField
            label="Ubicación del origen *"
            value={origin}
            linkValue={originLink}
            onChange={(c, link) => {
              setOrigin(c);
              setOriginLink(link);
            }}
          />
        </div>

        <div>
          <label className="cust-label">{tipo === 'compra' ? 'Notas para el rider (opcional)' : 'Describe el encargo *'}</label>
          <textarea
            className="cust-input"
            rows={3}
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            placeholder={
              tipo === 'compra'
                ? 'Marca,Presentaciones, referencias del lugar…'
                : 'Ej: 1 caja de documentos de la oficina de mi papá a la mía. Fragil.'
            }
          />
        </div>

        {tipo === 'compra' && (
          <div>
            <label className="cust-label">Artículos a comprar *</label>
            <div className="grid gap-3">
              {articulos.map((a, i) => (
                <div key={i} className="flex flex-col sm:flex-row gap-2">
                  <input
                    className="cust-input flex-[2] min-w-0"
                    placeholder="¿Qué artículo? (ej: leche 1L)"
                    value={a.name}
                    onChange={(e) => setArt(i, { name: e.target.value })}
                  />
                  <div className="flex gap-2 shrink-0">
                    <input
                      className="cust-input w-14 text-center"
                      type="number"
                      min={1}
                      value={a.quantity}
                      onChange={(e) => setArt(i, { quantity: Math.max(1, +e.target.value || 1) })}
                    />
                    <input
                      className="cust-input w-24"
                      placeholder="$ est."
                      inputMode="decimal"
                      value={a.price}
                      onChange={(e) => setArt(i, { price: e.target.value })}
                    />
                    {articulos.length > 1 && (
                      <button
                        className="px-2 self-center"
                        style={{ color: 'var(--danger)' }}
                        onClick={() => setArticulos((arr) => arr.filter((_, j) => j !== i))}
                        aria-label="Quitar"
                      >
                        ✕
                      </button>
                    )}
                  </div>
                </div>
              ))}
              <button
                className="cust-btn cust-btn-ghost text-sm"
                onClick={() => setArticulos((arr) => [...arr, { name: '', quantity: 1, price: '' }])}
              >
                + Otro artículo
              </button>
            </div>
          </div>
        )}

        {!canSend && (
          <p className="text-sm" style={{ color: 'var(--gold)' }}>
            {origin === null ? 'Primero pon la ubicación del origen.' : tipo === 'compra' ? 'Agrega al menos un artículo.' : 'Describe el encargo.'}
          </p>
        )}

        <button className="cust-btn cust-btn-primary" disabled={!canSend} onClick={() => setSheet(true)}>
          Continuar →
        </button>
      </div>

      {sheet && (
        <CheckoutSheet
          title={tipo === 'compra' ? 'Pedido de compra' : 'Pedido de encargo'}
          allowQr={false}
          onClose={() => setSheet(false)}
          summary={
            <div className="mb-4 text-sm" style={{ color: 'var(--cream-dim)' }}>
              <b style={{ color: 'var(--cream)' }}>
                {tipo === 'compra' ? '🛒 Compra' : '📦 Encargo'}
              </b>{' '}
              desde {originName.trim() || 'el punto indicado'} → hasta tu dirección.
              {description.trim() && <p className="mt-1">“{description.trim()}”</p>}
            </div>
          }
          onSubmit={async (v) => {
            const req: CreateOrderRequest = {
              ...v,
              type: tipo,
              originLat: origin!.lat,
              originLng: origin!.lng,
              originName: originName.trim() || undefined,
              description: description.trim() || undefined,
              items:
                tipo === 'compra'
                  ? articulos
                      .filter((a) => a.name.trim())
                      .map((a) => ({
                        name: a.name.trim(),
                        quantity: a.quantity,
                        unitPrice: parseFloat(a.price.replace(',', '.')) || 0,
                      }))
                  : undefined,
            };
            const { data } = await api.post<OrderTracking>('/orders', req);
            navigate(`/p/${token}/order/${data.id}`);
          }}
        />
      )}
    </div>
  );
}
