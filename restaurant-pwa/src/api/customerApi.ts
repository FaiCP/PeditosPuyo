import axios from 'axios';

const base = (import.meta.env.VITE_API_URL as string) || 'http://localhost:5080/api';

/**
 * Cliente para las páginas públicas del cliente final (/p/{token}).
 * Sin JWT: la identidad es el token del link. Un 404 aquí significa
 * "link inválido o expirado" — nunca redirige a /login.
 */
export function createCustomerApi(token: string) {
  return axios.create({ baseURL: `${base}/p/${encodeURIComponent(token)}` });
}

/** Resolve-link es un endpoint público del api principal (expande links cortos de Maps). */
export const linkResolver = axios.create({ baseURL: base.replace(/\/$/, '') });
