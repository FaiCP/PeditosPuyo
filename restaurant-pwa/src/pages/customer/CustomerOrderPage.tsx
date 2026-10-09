import { useEffect, useMemo, useRef, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { createCustomerApi } from '../../api/customerApi';
import type { OrderTracking } from '../../types/customer';
import { money } from '../../types/customer';
import { ACTIVE_STATUSES, CANCELABLE, STATUS_INFO, TIMELINE, TYPE_LABEL, fmtTime } from './status';
import { BrokenLink, useBrokenLink } from './CustomerCatalogPage';

export default function CustomerOrderPage() {
  const { token = '', orderId = '' } = useParams();
  const api = useMemo(() => createCustomerApi(token), [token]);
  const [order, setOrder] = useState<OrderTracking | null>(null);
  const [broken, onError] = useBrokenLink();
  const [busy, setBusy] = useState(false);
  const alive = useRef(true);

  useEffect(() => {
    alive.current = true;
    const load = () =>
      api
        .get<OrderTracking>(`/orders/${orderId}`)
        .then(({ data }) => setOrder(data))
        .catch(onError);
    load();
    const timer = setInterval(() => {
      if (alive.current) load();
    }, 5000);
    return () => {
      alive.current = false;
      clearInterval(timer);
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [api, orderId]);

  if (broken) return <BrokenLink />;
  if (!order) return <div className="cust-skeleton h-48 mt-6" />;

  const info = STATUS_INFO[order.status] ?? { es: order.status, badge: 'cust-badge-wait', step: -1 };
  const showPickupCode =
    order.pickupCode &&
    (order.type === 'compra' || order.type === 'encargo') &&
    (order.status === 'ReadyForPickup' || order.status === 'PickedUp' || order.status === 'InTransit');
  const showCode = order.deliveryCode && info.step >= 3;

  const cancel = async () => {
    setBusy(true);
    try {
      await api.post(`/orders/${order.id}/cancel`);
      const { data } = await api.get<OrderTracking>(`/orders/${order.id}`);
      setOrder(data);
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="cust-stagger">
      <Link to={`/p/${token}/mis-pedidos`} className="text-sm font-semibold" style={{ color: 'var(--cream-dim)' }}>
        ← Mis pedidos
      </Link>

      <div className="mt-3">
        <span className={`cust-badge ${info.badge}`}>{info.es}</span>
        <h1 className="cust-display text-3xl mt-3 mb-1">
          {TYPE_LABEL[order.type] ?? order.type} · {order.originName}
        </h1>
        <p className="text-sm" style={{ color: 'var(--cream-dim)' }}>
          🏠 {order.destinationAddress}
        </p>
      </div>

      {showPickupCode && (
        <div className="cust-card mt-5 p-5">
          <p className="text-xs uppercase tracking-widest font-bold" style={{ color: 'var(--cream-dim)' }}>
            Código de recolección — dáselo al rider en el origen
          </p>
          <p className="cust-code my-2">{order.pickupCode}</p>
        </div>
      )}

      {showCode && (
        <div className="cust-card mt-5 p-5">
          <p className="text-xs uppercase tracking-widest font-bold" style={{ color: 'var(--cream-dim)' }}>
            Tu código de entrega — dáselo al rider al recibir
          </p>
          <p className="cust-code my-2">{order.deliveryCode}</p>
        </div>
      )}

      {order.riderName && (
        <div className="cust-card mt-4 p-4 flex items-center justify-between gap-3">
          <div>
            <b>🛵 {order.riderName}</b>
            <p className="text-xs" style={{ color: 'var(--cream-dim)' }}>
              Tu rider asignado
            </p>
          </div>
          {order.riderLat != null && order.riderLng != null && (
            <a
              className="cust-btn cust-btn-ghost text-sm whitespace-nowrap"
              href={`https://www.google.com/maps?q=${order.riderLat},${order.riderLng}`}
              target="_blank"
              rel="noreferrer"
            >
              Ver en mapa
            </a>
          )}
        </div>
      )}

      <div className="cust-card mt-4 p-5">
        <div className="cust-tl">
          {TIMELINE.map((t) => {
            const done = info.step >= t.step && info.step >= 0;
            const now = info.step === t.step && ACTIVE_STATUSES.has(order.status);
            return (
              <div key={t.step} className={`cust-tl-item ${now ? 'now' : done ? 'done' : ''}`}>
                <b className="text-sm" style={{ opacity: done ? 1 : 0.45 }}>
                  {t.label}
                </b>
              </div>
            );
          })}
          {(info.step === -1 || order.status === 'ReadyForPickup') && (
            <div className="cust-tl-item now">
              <b className="text-sm">{info.es}</b>
            </div>
          )}
        </div>
        <p className="text-xs mt-1" style={{ color: 'var(--cream-dim)' }}>
          Pedido enviado {fmtTime(order.submittedAt || order.createdAt)} · se actualiza solo
        </p>
      </div>

      {order.items.length > 0 && (
        <div className="cust-card mt-4 p-4">
          {order.items.map((i, j) => (
            <div key={j} className="flex justify-between text-sm py-1">
              <span>
                {i.quantity}× {i.name}
              </span>
              <span className="cust-price">{money(i.quantity * i.unitPrice)}</span>
            </div>
          ))}
          {order.description && (
            <p className="text-xs mt-2 italic" style={{ color: 'var(--cream-dim)' }}>
              “{order.description}”
            </p>
          )}
          <div className="border-t mt-3 pt-2" style={{ borderColor: 'var(--card-edge)' }}>
            <div className="flex justify-between text-xs" style={{ color: 'var(--cream-dim)' }}>
              <span>Productos</span>
              <span>{money(order.productsAmount)}</span>
            </div>
            <div className="flex justify-between text-xs" style={{ color: 'var(--cream-dim)' }}>
              <span>Tarifa de delivery</span>
              <span>{money(order.deliveryFeeAmount)}</span>
            </div>
            <div className="flex justify-between font-bold mt-1">
              <span>Total a pagar al recibir</span>
              <span className="cust-price text-lg">{money(order.totalAmount)}</span>
            </div>
          </div>
        </div>
      )}

      {CANCELABLE.has(order.status) && (
        <button className="cust-btn w-full mt-4 text-sm" style={{ color: 'var(--danger)', border: '1px solid var(--danger)' }} onClick={cancel} disabled={busy}>
          {busy ? 'Cancelando…' : 'Cancelar pedido'}
        </button>
      )}
      {order.cancelDetail && <p className="text-sm mt-3" style={{ color: 'var(--danger)' }}>{order.cancelDetail}</p>}
    </div>
  );
}
