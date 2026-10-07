import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { createCustomerApi } from '../../api/customerApi';
import type { PublicRestaurant } from '../../types/customer';

export function useBrokenLink(): [boolean, (e: unknown) => void] {
  const [broken, setBroken] = useState(false);
  const onError = (e: unknown) => {
    if ((e as { response?: { status?: number } }).response?.status === 404) setBroken(true);
  };
  return [broken, onError];
}

export function BrokenLink() {
  return (
    <div className="text-center py-20">
      <p className="cust-display text-3xl mb-3" style={{ color: 'var(--danger)' }}>
        Link inválido o expirado
      </p>
      <p style={{ color: 'var(--cream-dim)' }}>
        Pídele a la empresa de delivery un link nuevo. Los links duran 7 días.
      </p>
    </div>
  );
}

export default function CustomerCatalogPage() {
  const { token = '' } = useParams();
  const [restaurants, setRestaurants] = useState<PublicRestaurant[]>([]);
  const [search, setSearch] = useState('');
  const [loading, setLoading] = useState(true);
  const [broken, onError] = useBrokenLink();

  useEffect(() => {
    const api = createCustomerApi(token);
    api
      .get<PublicRestaurant[]>('/catalog')
      .then(({ data }) => setRestaurants(data))
      .catch(onError)
      .finally(() => setLoading(false));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [token]);

  if (broken) return <BrokenLink />;

  const q = search.trim().toLowerCase();
  const list = q
    ? restaurants.filter(
        (r) =>
          r.name.toLowerCase().includes(q) ||
          r.address.toLowerCase().includes(q) ||
          (r.menuSummary ?? '').toLowerCase().includes(q)
      )
    : restaurants;

  return (
    <div>
      <h1 className="cust-display text-[34px] leading-[1.05] mt-6 mb-1">
        ¿Qué se te antoja
        <br />
        <span style={{ color: 'var(--tang)' }}>hoy, jefe?</span>
      </h1>
      <p className="mb-5" style={{ color: 'var(--cream-dim)' }}>
        Elige un restaurante o pide un mandado / compra.
      </p>

      <div className="grid grid-cols-2 gap-3 mb-6">
        <Link to={`/p/${token}/nuevo?tipo=compra`} className="cust-card p-4 flex flex-col gap-2">
          <span className="text-3xl">🛒</span>
          <b className="cust-display text-lg">Compra</b>
          <span className="text-xs" style={{ color: 'var(--cream-dim)' }}>
            El rider compra y te trae
          </span>
        </Link>
        <Link to={`/p/${token}/nuevo?tipo=encargo`} className="cust-card p-4 flex flex-col gap-2">
          <span className="text-3xl">📦</span>
          <b className="cust-display text-lg">Encargo</b>
          <span className="text-xs" style={{ color: 'var(--cream-dim)' }}>
            Llevar o traer un paquete
          </span>
        </Link>
      </div>

      <input
        className="cust-input mb-5"
        placeholder={`Buscar entre ${restaurants.length} lugares…`}
        value={search}
        onChange={(e) => setSearch(e.target.value)}
      />

      {loading ? (
        <div className="grid gap-3">
          {[0, 1, 2, 3].map((i) => (
            <div key={i} className="cust-skeleton h-24" />
          ))}
        </div>
      ) : list.length === 0 ? (
        <p className="py-10 text-center" style={{ color: 'var(--cream-dim)' }}>
          Nada por aquí… prueba otra palabra.
        </p>
      ) : (
        <div className="grid gap-3 cust-stagger">
          {list.map((r) => (
            <Link key={r.id} to={`/p/${token}/r/${r.id}`} className="cust-card p-4 flex items-center gap-4">
              <div
                className="shrink-0 w-14 h-14 rounded-2xl flex items-center justify-center text-2xl"
                style={{ background: 'var(--ink)' }}
              >
                {r.itemCount > 0 ? '🍽️' : '🕒'}
              </div>
              <div className="min-w-0">
                <b className="cust-display text-[17px] block truncate">{r.name}</b>
                <span className="text-sm block truncate" style={{ color: 'var(--cream-dim)' }}>
                  📍 {r.address}
                </span>
                <span className="text-xs" style={{ color: 'var(--leaf)' }}>
                  {r.itemCount > 0 ? `${r.itemCount} en el menú` : 'Menú pronto — puedes llamar'}
                  {r.hasPaymentQr && ' · paga por QR'}
                </span>
              </div>
            </Link>
          ))}
        </div>
      )}
    </div>
  );
}
