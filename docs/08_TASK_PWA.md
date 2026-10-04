# Task PWA - React + Vite + Tailwind

## Modelo: Qwen3.7 Plus

## Estructura del Proyecto

```
restaurant-pwa/
├── public/
│   ├── manifest.json
│   ├── sw.js
│   └── sounds/
│       └── new-order.mp3
├── src/
│   ├── main.tsx
│   ├── App.tsx
│   ├── index.css
│   ├── api/
│   │   ├── client.ts          # Axios instance con JWT interceptor
│   │   ├── restaurants.ts
│   │   ├── deliveryRequests.ts
│   │   └── auth.ts
│   ├── hooks/
│   │   ├── useAuth.ts
│   │   ├── useSignalR.ts
│   │   └── useSoundAlert.ts
│   ├── pages/
│   │   ├── PublicCatalogPage.tsx    # Listado público restaurantes
│   │   ├── RestaurantDetailPage.tsx # Detalle restaurante + menú
│   │   ├── LoginPage.tsx
│   │   ├── DashboardPage.tsx        # Panel restaurante
│   │   └── NewDeliveryPage.tsx      # Solicitar delivery
│   ├── components/
│   │   ├── RestaurantCard.tsx
│   │   ├── MenuList.tsx
│   │   ├── DeliveryRequestForm.tsx
│   │   ├── ActiveRequestCard.tsx
│   │   ├── RiderTracker.tsx
│   │   └── Layout.tsx
│   ├── context/
│   │   └── AuthContext.tsx
│   └── types/
│       └── index.ts
├── index.html
├── package.json
├── vite.config.ts
├── tailwind.config.js
└── tsconfig.json
```

## Páginas Detalladas

### 1. PublicCatalogPage (sin login)
- Listado de restaurantes: nombre, dirección, teléfono, menú resumido.
- Búsqueda por nombre.
- Click en restaurante → RestaurantDetailPage.
- Botón "Llamar" que abre el teléfono (`tel:` link).

### 2. RestaurantDetailPage
- Info del restaurante.
- Lista de menú items (solo lectura).
- Botón para solicitar delivery (requiere login de restaurante).

### 3. LoginPage
- Email + password.
- Llama a `POST /auth/login`.
- Guarda token + user en AuthContext.
- Redirige según rol.

### 4. DashboardPage (RestaurantAdmin)
- Lista de delivery requests activos.
- Botón "Nuevo Delivery".
- Sonido de alerta cuando llega un nuevo request (Web Audio API).
- Actualización en tiempo real vía SignalR.

### 5. NewDeliveryPage
- Formulario: dirección, coordenadas (GPS o input manual), notas.
- Botón para obtener ubicación actual.
- Submit → `POST /delivery-requests`.
- Redirige al dashboard con el request creado.

## Dependencias

```json
{
  "dependencies": {
    "react": "^18.3.0",
    "react-dom": "^18.3.0",
    "react-router-dom": "^6.26.0",
    "axios": "^1.7.0",
    "@microsoft/signalr": "^8.0.0"
  },
  "devDependencies": {
    "vite": "^5.4.0",
    "@vitejs/plugin-react": "^4.3.0",
    "tailwindcss": "^3.4.0",
    "autoprefixer": "^10.4.0",
    "postcss": "^8.4.0",
    "typescript": "^5.5.0"
  }
}
```

## Web Audio API - Sonido de Alerta

```typescript
// useSoundAlert.ts
export function useSoundAlert() {
  const playAlert = () => {
    const audioContext = new AudioContext();
    const oscillator = audioContext.createOscillator();
    const gainNode = audioContext.createGain();
    
    oscillator.connect(gainNode);
    gainNode.connect(audioContext.destination);
    
    oscillator.frequency.value = 800;
    oscillator.type = 'sine';
    gainNode.gain.value = 0.3;
    
    oscillator.start();
    setTimeout(() => oscillator.stop(), 500);
    
    // Repetir 3 veces
    setTimeout(() => {
      const osc2 = audioContext.createOscillator();
      osc2.connect(gainNode);
      osc2.frequency.value = 800;
      osc2.start();
      setTimeout(() => osc2.stop(), 500);
    }, 700);
  };
  
  return { playAlert };
}
```

## Manifest.json (PWA)

```json
{
  "name": "Puyo Delivery - Restaurantes",
  "short_name": "PuyoDelivery",
  "start_url": "/",
  "display": "standalone",
  "background_color": "#ffffff",
  "theme_color": "#1971c2",
  "icons": [
    { "src": "/icon-192.png", "sizes": "192x192", "type": "image/png" },
    { "src": "/icon-512.png", "sizes": "512x512", "type": "image/png" }
  ]
}
```

## Deploy en Vercel

```bash
npm run build
# Output: dist/
# Vercel detecta automáticamente Vite
```

Configuración Vercel:
- Framework: Vite
- Build command: `npm run build`
- Output directory: `dist`
- Environment variables: `VITE_API_URL=https://tu-backend.onrender.com/api`
