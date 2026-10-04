# Puyo Delivery Engine - B2B2C

Plataforma de red de delivery. **Fase 1:** SaaS B2B para empresas de delivery. **Fase 2:** Red B2B2C con restaurantes y clientes finales.

## Estructura del Proyecto

```
puyo-b2b2c-delivery/
├── backend/              # ASP.NET Core 9 Web API + SignalR + EF Core
│   ├── PuyoDelivery.API/
│   ├── PuyoDelivery.Core/
│   ├── PuyoDelivery.Infrastructure/
│   └── PuyoDelivery.Tests/
├── restaurant-pwa/       # React + Vite + Tailwind (PWA para restaurantes)
├── rider_app/            # Flutter APK (app del rider)
└── docs/                 # Documentación del proyecto
```

## Stack Tecnológico

| Capa | Tecnología |
|------|-----------|
| Backend | ASP.NET Core 9 + SignalR + EF Core |
| DB | PostgreSQL 15 + PostGIS |
| PWA | React + Vite + Tailwind CSS |
| Rider App | Flutter |
| Hosting Backend | Render (free tier) |
| Hosting PWA | Vercel (free tier) |
| Push | Firebase Cloud Messaging |
| Geocoding | OpenStreetMap Nominatim |

## Inicio Rápido

### Backend

```bash
cd backend
dotnet restore
dotnet build

# Necesitas PostgreSQL corriendo con PostGIS habilitado
# Crear base de datos:
# CREATE EXTENSION IF NOT EXISTS postgis;

dotnet run --project PuyoDelivery.API
```

Swagger: `http://localhost:5001/swagger`

### PWA

```bash
cd restaurant-pwa
npm install
npm run dev
```

URL: `http://localhost:5173`

### Rider App (Flutter)

```bash
cd rider_app
flutter pub get
flutter run

# Build APK para distribuir:
flutter build apk --release
```

## Flujo Fase 1

1. Cliente ve catálogo de restaurantes (PWA pública, sin login).
2. Cliente llama/WhatsAppea al restaurante y confirma pedido.
3. Restaurante ingresa a la PWA y solicita servicio de delivery.
4. Admin de empresa de delivery asigna manualmente un rider.
5. Rider acepta y entrega. Tracking en tiempo real.

## Modelos IA por Fase

| Fase | Modelo | Cap mensual |
|------|--------|-------------|
| Documentación | GLM-5.3-Flash | $60 |
| Backend .NET | Kimi K2.7 Code | $60 |
| PWA | Qwen3.7 Plus | $60 |
| Flutter | Qwen3.7 Plus | $60 |
| Tests | Por definir | — |

Plan de referencia: OpenCode Go ($10/mes) o Go Plus ($40/mes).

## Documentación

Ver carpeta `docs/` para especificación completa:

- `00_VISION_B2B2C.md` - Modelo de negocio
- `01_STACK_Y_HOSTING.md` - Stack tecnológico
- `02_MODELO_DATOS.md` - Entidades y base de datos
- `03_API_CONTRACT.md` - Contrato de API y SignalR
- `04_FLUJO_FASE1.md` - Flujo paso a paso
- `05_ROLES_AUTH.md` - Autenticación y autorización
- `06_TASK_BACKEND.md` - Tareas backend
- `07_TASK_FLUTTER.md` - Tareas Flutter
- `08_TASK_PWA.md` - Tareas PWA
- `09_ROADMAP.md` - Roadmap por fases
- `10_ARQUITECTURA_Y_PATRONES.md` - Arquitectura y patrones
- `11_MODELOS_POR_FASE.md` - Asignación de modelos IA
