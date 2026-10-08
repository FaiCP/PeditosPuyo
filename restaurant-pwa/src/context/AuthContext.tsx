import { createContext, useContext, useState, useEffect } from 'react';
import type { ReactNode } from 'react';
import api from '../api/client';
import type { User, LoginResponse } from '../types';

export interface RegisterData {
  fullName: string;
  email: string;
  phone: string;
  password: string;
  role: string;
  restaurantName?: string;
  restaurantAddress?: string;
  restaurantPhone?: string;
  logoUrl?: string;
  initialMenuItems?: { name: string; description?: string; price: number; imageUrl?: string }[];
}

interface AuthContextType {
  user: User | null;
  token: string | null;
  login: (email: string, password: string) => Promise<void>;
  register: (data: RegisterData) => Promise<void>;
  logout: () => void;
  isAuthenticated: boolean;
}

const AuthContext = createContext<AuthContextType | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [token, setToken] = useState<string | null>(localStorage.getItem('token'));

  useEffect(() => {
    const savedUser = localStorage.getItem('user');
    if (savedUser) setUser(JSON.parse(savedUser));
  }, []);

  const setAuth = (data: LoginResponse) => {
    localStorage.setItem('token', data.token);
    const userData: User = {
      id: data.userId,
      email: data.email,
      fullName: data.fullName,
      role: data.role,
      tenantId: data.tenantId,
      restaurantId: data.restaurantId,
      companyId: data.companyId,
    };
    localStorage.setItem('user', JSON.stringify(userData));
    setToken(data.token);
    setUser(userData);
  };

  const login = async (email: string, password: string) => {
    const { data } = await api.post<LoginResponse>('/auth/login', { email, password });
    setAuth(data);
  };

  const register = async (data: RegisterData) => {
    const { data: response } = await api.post<LoginResponse>('/auth/register', {
      fullName: data.fullName,
      email: data.email,
      phone: data.phone,
      password: data.password,
      role: data.role,
      restaurantName: data.restaurantName,
      restaurantAddress: data.restaurantAddress,
      restaurantPhone: data.restaurantPhone,
      logoUrl: data.logoUrl,
      initialMenuItems: data.initialMenuItems,
    });
    setAuth(response);
  };

  const logout = () => {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    setToken(null);
    setUser(null);
    window.location.href = '/login';
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        token,
        login,
        register,
        logout,
        isAuthenticated: !!token,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) throw new Error('useAuth must be used within AuthProvider');
  return context;
}
