# Visión B2B2C - Puyo Delivery Engine

## Modelo de negocio

Plataforma de red de delivery. **Fase 1: B2B** (vender SaaS a empresas de delivery). **Fase 2: B2B2C** (abrir red a restaurantes y clientes finales).

## Actores

| Actor | Rol | Acceso |
|-------|-----|--------|
| SuperAdmin | Tu plataforma | Admin global |
| CompanyAdmin | Empresa de delivery | Admin flota, asignación, suscripción |
| RestaurantAdmin | Restaurante registrado | Solicitar delivery, ver estado |
| Rider | Motorizado | Aceptar/rechazar carreras, GPS |
| Customer | Cliente final | **Fase 2**: ver catálogo, checkout |

## Flujo Fase 1

1. Cliente ve catálogo de restaurantes (PWA pública, sin login).
2. Cliente llama/WhatsAppea al restaurante y confirma pedido.
3. Restaurante ingresa a la PWA y solicita servicio de delivery.
4. Admin de empresa de delivery asigna manualmente un rider.
5. Rider acepta y entrega. Tracking en tiempo real.

## Flujo Fase 2

1. Cliente hace pedido desde PWA/app con checkout.
2. Plataforma notifica empresas de delivery disponibles.
3. Asignación automática por cercanía (PostGIS).
4. Rider acepta, entrega, califica.

## Ingresos

- **Fase 1:** Empresa de delivery paga suscripción mensual.
- **Fase 2:** + comisión por delivery, créditos para promociones.

## Stack

- Backend: ASP.NET Core 9 + SignalR + EF Core + PostgreSQL/PostGIS
- PWA: React + Vite + Tailwind (Vercel)
- Rider app: Flutter APK
- Hosting backend: Render free tier
