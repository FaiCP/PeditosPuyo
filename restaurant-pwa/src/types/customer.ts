// Modelos de la Fase 2 para el cliente final (contrato /api/p/{token})

export interface PublicRestaurant {
  id: string;
  name: string;
  address: string;
  phone: string;
  menuSummary?: string | null;
  itemCount: number;
  hasPaymentQr: boolean;
}

export interface PublicMenuItem {
  id: string;
  name: string;
  description?: string | null;
  price: number;
}

export interface PublicRestaurantDetail {
  id: string;
  name: string;
  address: string;
  phone: string;
  paymentQrUrl?: string | null;
  menu: PublicMenuItem[];
}

export interface OrderItemLine {
  menuItemId?: string | null;
  name: string;
  quantity: number;
  unitPrice: number;
  notes?: string | null;
}

export interface CreateOrderRequest {
  type: 'restaurant' | 'compra' | 'encargo';
  restaurantId?: string | null;
  originLat?: number | null;
  originLng?: number | null;
  originName?: string | null;
  originAddress?: string | null;
  destinationAddress: string;
  destinationLat: number;
  destinationLng: number;
  destinationLinkRaw?: string | null;
  description?: string | null;
  items?: OrderItemLine[] | null;
  customerName: string;
  customerPhone: string;
  paymentMethod: 'cash' | 'restaurantQr';
}

export interface OrderTrackingItem {
  name: string;
  quantity: number;
  unitPrice: number;
}

export interface OrderTracking {
  id: string;
  type: string;
  status: string;
  originName: string;
  originAddress: string;
  destinationAddress: string;
  description?: string | null;
  productsAmount: number;
  deliveryFeeAmount: number;
  totalAmount: number;
  paymentMethod: string;
  hasPaymentQr: boolean;
  paymentQrUrl?: string | null;
  pickupCode?: string | null;
  deliveryCode?: string | null;
  riderName?: string | null;
  riderLat?: number | null;
  riderLng?: number | null;
  cancelDetail?: string | null;
  createdAt: string;
  submittedAt?: string | null;
  riderAcceptedAt?: string | null;
  restaurantConfirmedAt?: string | null;
  deliveredAt?: string | null;
  items: OrderTrackingItem[];
}

export function money(n: number): string {
  return `$${n.toFixed(2)}`;
}
