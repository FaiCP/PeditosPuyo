export interface Restaurant {
  id: string;
  name: string;
  slug: string;
  address: string;
  phone: string;
  lat: number;
  lng: number;
  menuSummary?: string;
  isActive: boolean;
  source: string;
}

export interface MenuItem {
  id: string;
  name: string;
  description?: string;
  price: number;
  isActive: boolean;
}

export interface DeliveryRequest {
  id: string;
  restaurantId: string;
  restaurantName: string;
  status: string;
  deliveryAddress: string;
  lat: number;
  lng: number;
  notes?: string;
  createdAt: string;
  assignedAt?: string;
  acceptedAt?: string;
  deliveredAt?: string;
}

export interface User {
  id: string;
  email: string;
  fullName: string;
  role: string;
  tenantId?: string;
}

export interface LoginResponse {
  token: string;
  userId: string;
  role: string;
  tenantId?: string;
  fullName: string;
  email: string;
}
