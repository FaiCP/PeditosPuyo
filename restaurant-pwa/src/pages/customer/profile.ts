const KEY = 'peditos-customer';

export function savedProfile(): { name: string; phone: string } {
  try {
    const raw = localStorage.getItem(KEY);
    if (raw) return JSON.parse(raw) as { name: string; phone: string };
  } catch {
    /* almacenamiento sucio → ignorar */
  }
  return { name: '', phone: '' };
}

export function rememberProfile(name: string, phone: string) {
  try {
    localStorage.setItem(KEY, JSON.stringify({ name, phone }));
  } catch {
    /* navegador en modo privado */
  }
}
