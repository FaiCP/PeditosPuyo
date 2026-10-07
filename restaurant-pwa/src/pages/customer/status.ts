export interface StatusInfo {
  es: string;
  badge: string;
  step: number; // -1 cancelado/hold, 0..5 avance en la línea de tiempo
}

// Máquina de estados del Order (docs/12_PLAN_FASE_2.md §3)
export const STATUS_INFO: Record<string, StatusInfo> = {
  WaitingRider: { es: 'Buscando rider cercano…', badge: 'cust-badge-wait', step: 0 },
  RiderAccepted: { es: '¡Rider aceptó! Va al punto de origen', badge: 'cust-badge-go', step: 1 },
  ReadyForPickup: { es: 'Listo en el origen — el rider recoge', badge: 'cust-badge-go', step: 2 },
  PickedUp: { es: 'Recogido — va hacia ti', badge: 'cust-badge-go', step: 3 },
  InTransit: { es: 'En camino a tu dirección', badge: 'cust-badge-go', step: 4 },
  Delivered: { es: 'Entregado ✓', badge: 'cust-badge-done', step: 5 },
  OnHold: { es: 'Sin riders disponibles por ahora', badge: 'cust-badge-bad', step: -1 },
  Cancelled: { es: 'Cancelado', badge: 'cust-badge-bad', step: -1 },
};

export const TIMELINE: { step: number; label: string }[] = [
  { step: 0, label: 'Pedido enviado' },
  { step: 1, label: 'Rider asignado' },
  { step: 2, label: 'Recogido en el origen' },
  { step: 4, label: 'En camino a ti' },
  { step: 5, label: 'Entregado' },
];

export const ACTIVE_STATUSES = new Set(['WaitingRider', 'RiderAccepted', 'ReadyForPickup', 'PickedUp', 'InTransit']);
export const CANCELABLE = new Set(['WaitingRider', 'RiderAccepted', 'ReadyForPickup', 'OnHold']);

export const TYPE_LABEL: Record<string, string> = {
  restaurant: '🍔 Restaurante',
  compra: '🛒 Compra',
  encargo: '📦 Encargo',
};

export function fmtTime(iso?: string | null): string {
  if (!iso) return '';
  const d = new Date(iso.endsWith('Z') || iso.includes('+') ? iso : iso + 'Z');
  return d.toLocaleTimeString('es-EC', { hour: '2-digit', minute: '2-digit' });
}
