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

export interface Rider {
  id: string;
  fullName: string;
  phone: string;
  vehiclePlate: string;
  isOnline: boolean;
  isBusy: boolean;
  lat?: number;
  lng?: number;
  distanceKm?: number;
}

export interface User {
  id: string;
  email: string;
  fullName: string;
  role: string;
  tenantId?: string;
  restaurantId?: string;
  companyId?: string;
}

export interface LoginResponse {
  token: string;
  userId: string;
  role: string;
  tenantId?: string;
  fullName: string;
  email: string;
  companyId?: string;
  restaurantId?: string;
  riderId?: string;
}

export interface ImportResult {
  imported: number;
  errors: string[];
}
