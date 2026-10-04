# Roadmap - Puyo B2B2C Delivery

## Fase 1 - MVP B2B (Actual)

**Duración estimada:** 4-6 semanas

### Semana 1-2: Fundamentos
- [x] Documentación de especificación
- [ ] Backend: solución .NET + entidades + migrations
- [ ] Backend: Auth JWT + multi-tenancy
- [ ] Backend: CRUD empresas + riders

### Semana 3-4: Core
- [ ] Backend: CRUD restaurantes + import
- [ ] Backend: Delivery requests + assignments
- [ ] Backend: SignalR Hubs
- [ ] Backend: Stored procedure nearest riders
- [ ] PWA: Página pública de catálogo
- [ ] PWA: Login + dashboard restaurante
- [ ] PWA: Solicitar delivery + sonido de alerta

### Semana 5-6: App Rider + Integración
- [ ] Flutter: Estructura + auth + GPS background
- [ ] Flutter: FCM + modal de orden
- [ ] Flutter: Integración SignalR
- [ ] Testing integración punta a punta
- [ ] Deploy: Render + Vercel + APK

### Entregable Fase 1
- Empresa de delivery registrada con sus riders.
- Restaurantes importados (OSM o manual).
- Flujo completo: restaurante pide delivery → admin asigna → rider entrega.
- Tracking en tiempo real.

---

## Fase 2 - B2B2C (Futuro)

**Duración estimada:** 6-8 semanas

### Nuevos componentes
- [ ] Checkout de cliente final (carrito + pago)
- [ ] Sistema de créditos para promociones
- [ ] Asistente de promociones (IA)
- [ ] Asignación automática de riders (PostGIS)
- [ ] App cliente (Flutter o PWA)
- [ ] Calificaciones y reseñas
- [ ] Múltiples empresas de delivery compitiendo
- [ ] Métricas y analytics avanzados

### Nuevos modelos de ingresos
- [ ] Comisión por delivery
- [ ] Créditos de promoción (venta directa)
- [ ] Planes premium para restaurantes

---

## Fase 3 - Escalamiento (Futuro lejano)

- [ ] Migración a AWS (RDS, ECS/Fargate, ElastiCache)
- [ ] Play Store (APK oficial)
- [ ] Múltiples ciudades
- [ ] App nativa para restaurantes (opcional)
- [ ] API pública para terceros
- [ ] Multi-idioma
- [ ] Monedas múltiples

---

## Stack Tecnológico por Fase

| Fase | Backend | Frontend | App Rider | DB |
|------|---------|----------|-----------|-----|
| 1 | ASP.NET Core 9 | React PWA | Flutter | PostgreSQL+PostGIS |
| 2 | ASP.NET Core 9 | React PWA + App Cliente | Flutter | PostgreSQL+PostGIS |
| 3 | ASP.NET Core 9 / ECS | React PWA + App Cliente | Flutter | RDS PostgreSQL |

---

## Modelo de IA por Fase

| Fase | Modelo | Cap mensual |
|------|--------|-------------|
| Documentación | GLM-5.3-Flash | $60 |
| Backend .NET | Kimi K2.7 Code | $60 |
| PWA | Qwen3.7 Plus | $60 |
| Flutter | Qwen3.7 Plus | $60 |
| Tests | Por definir | — |
