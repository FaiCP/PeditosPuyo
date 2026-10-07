import { useEffect, useMemo, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { createCustomerApi } from '../../api/customerApi';
import type { OrderTracking } from '../../types/customer';
import { money } from '../../types/customer';
import { STATUS_INFO, TYPE_LABEL } from './status';
import { BrokenLink, useBrokenLink } from './CustomerCatalogPage';

export default function CustomerOrdersPage() {
  const { token = '' } = useParams();
  const api = useMemo(() => createCustomerApi(token), [token]);
  const [orders, setOrders] = useState<OrderTracking[]>([]);
  const [loading, setLoading] = useState(true);
  const [broken, onError] = useBrokenLink();

  useEffect(() => {
    api
      .get<OrderTracking[]>('/orders')
      .then(({ data }) => setOrders(data))
      .catch(onError)
      .finally(() => setLoading(false));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [api]);

  if (broken) return <BrokenLink />;

  return (
    <div>
      <h1 className="cust-display text-3xl mt-6 mb-5">Mis pedidos</h1>

      {loading ? (
        <div className="grid gap-3">
          {[0, 1].map((i) => (
            <div key={i} className="cust-skeleton h-24" />
          ))}
        </div>
      ) : orders.length === 0 ? (
        <div className="text-center py-16">
          <p className="text-4xl mb-3">🌴</p>
          <p style={{ color: 'var(--cream-dim)' }}>
            Todavía no hay pedidos. <Link to={`/p/${token}`} className="font-bold underline" style={{ color: 'var(--tang)' }}>Empieza uno</Link>.
          </p>
        </div>
      ) : (
        <div className="grid gap-3 cust-stagger">
          {orders.map((o) => {
            const info = STATUS_INFO[o.status] ?? { es: o.status, badge: 'cust-badge-wait' };
            return (
              <Link key={o.id} to={`/p/${token}/order/${o.id}`} className="cust-card p-4">
                <div className="flex items-center justify-between gap-3">
                  <b className="cust-display text-[17px] truncate">
                    {TYPE_LABEL[o.type] ?? o.type} · {o.originName}
                  </b>
                  <span className={`cust-badge ${info.badge} shrink-0`}>{info.es}</span>
                </div>
                <div className="flex justify-between text-xs mt-1" style={{ color: 'var(--cream-dim)' }}>
                  <span>{new Date(o.createdAt + (o.createdAt.endsWith('Z') ? '' : 'Z')).toLocaleDateString('es-EC')}</span>
                  <span>{money(o.totalAmount)}</span>
                </div>
              </Link>
            );
          })}
        </div>
      )}
    </div>
  );
}
