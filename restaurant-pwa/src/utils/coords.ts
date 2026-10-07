import { linkResolver } from '../api/customerApi';

export interface Coords {
  lat: number;
  lng: number;
}

const toNum = (v: string) => parseFloat(v.replace(',', '.'));

/** Extrae [lat, lng] de un link de Google Maps en varios formatos, o de "lat, lng" directo. */
export function parseCoords(input: string): Coords | null {
  const s = input.trim();
  if (!s) return null;

  const direct = s.match(/^\s*(-?\d+(?:[.,]\d+)?)\s*[, ]\s*(-?\d+(?:[.,]\d+)?)\s*$/);
  if (direct) return { lat: toNum(direct[1]), lng: toNum(direct[2]) };

  const at = s.match(/@(-?\d+(?:\.\d+)?),(-?\d+(?:\.\d+)?)/);
  if (at) return { lat: +at[1], lng: +at[2] };

  const q = s.match(/[?&](?:q|query)=(-?\d+(?:\.\d+)?),(-?\d+(?:\.\d+)?)/);
  if (q) return { lat: +q[1], lng: +q[2] };

  const ll = s.match(/[?&]lat=(-?\d+(?:\.\d+)?)[&].*?lng=(-?\d+(?:\.\d+)?)/i);
  if (ll) return { lat: +ll[1], lng: +ll[2] };

  const data = s.match(/!3d(-?\d+(?:\.\d+)?)!4d(-?\d+(?:\.\d+)?)/);
  if (data) return { lat: +data[1], lng: +data[2] };

  const place = s.match(/\/(-?\d+(?:\.\d+)?),(-?\d+(?:\.\d+)?)\/data/);
  if (place) return { lat: +place[1], lng: +place[2] };

  return null;
}

export function inBounds(c: Coords): boolean {
  return c.lat >= -90 && c.lat <= 90 && c.lng >= -180 && c.lng <= 180;
}

/** Expande links cortos de Google Maps (maps.app.goo.gl) vía el backend. */
export async function resolveShortLink(url: string): Promise<string> {
  if (!/^https?:\/\/(maps\.app\.goo\.gl|goo\.gl|g\.co)\//i.test(url)) return url;
  const { data } = await linkResolver.get<{ finalUrl: string }>('/resolve-link', { params: { url } });
  return data.finalUrl;
}

/** Convierte un link de Maps (o "lat,lng") en coordenadas; lanza Error con mensaje amigable. */
export async function coordsFromInput(input: string): Promise<Coords> {
  let target = input.trim();
  if (!target) throw new Error('Escribe el link de Google Maps o las coordenadas.');
  if (/^https?:\/\/(maps\.app\.goo\.gl|goo\.gl|g\.co)\//i.test(target)) {
    try {
      target = await resolveShortLink(target);
    } catch {
      throw new Error('No pude abrir el link corto. Pega la URL larga de Google Maps o escribe "lat, lng".');
    }
  }
  const coords = parseCoords(target);
  if (!coords) throw new Error('No encontré coordenadas en ese link. En Google Maps: Buscar → Compartir → Copiar link.');
  if (!inBounds(coords)) throw new Error('Coordenadas fuera de rango. Revisa el link.');
  return coords;
}

export function mapsUrl(c: Coords): string {
  return `https://www.google.com/maps?q=${c.lat},${c.lng}`;
}

/** Ubicación actual del navegador (promesa). */
export function currentLocation(): Promise<Coords> {
  return new Promise((resolve, reject) => {
    if (!navigator.geolocation) return reject(new Error('Este navegador no soporta geolocalización.'));
    navigator.geolocation.getCurrentPosition(
      (p) => resolve({ lat: p.coords.latitude, lng: p.coords.longitude }),
      (e) => reject(new Error(`No se pudo obtener tu ubicación: ${e.message}`)),
      { enableHighAccuracy: true, timeout: 12000 }
    );
  });
}
