# Plan Fase 2 — Plataforma de Pedidos B2C con Asignación Automática

**Modelo IA asignado:** Kimi K3 (planes/arquitectura) + Kimi K2.7 Code (backend) + Qwen3.7 Plus (fronts)

---

## 1. Visión de producto

Cambiar el modelo actual (el **restaurante** pide el delivery) al modelo que definiste:
el **cliente final** hace el pedido a través de un link que la **company** le envía.
La company coordina; la plataforma asigna riders.

### Actores nuevos
| Actor | Descripción | Login |
|-------|-------------|-------|
| **Customer** | Cliente final que recibe el link de la company | Sin contraseña — token en el link (7 días exp) |

### Flujo principal (happy path)

```
Company ──crea link──▶ Customer (link en WhatsApp)
   │
   ▼
CATÁLOGO PÚBLICO (restaurantes existentes)
   │
   ├── (A) Elije restaurante ─▶ pide del MENÚ (productos + precios)
   │
   └── (B) Pedido personalizado:
         • Link de ubicación (Google Maps → parser que ya existe)
         • Toggle: ENCARGO (llevar/traer paquete) | COMPRA (comprar algo)
         • COMPRA → lista de artículos con precios estimados
         • ENCARGO → descripción + foto opcional
   │
   ▼
ASIGNACIÓN AUTOMÁTICA DE RIDER (no elige el usuario)
   • Candidato: rider online + libre MÁS CERCANO al punto de origen
     (restaurante o ubicación del encargo)
   • Notificación INSISTENTE al rider (push + SignalR + SMS si hay)
     cada 30s, máx 3 min
   • Timeout o rechazo → siguiente rider más cercano (round-robin)
   • Sin nadie disponible → cola visible + alerta a la company
   │
   ▼
EL RIDER ACEFTA → "luz verde"
   • Se genera CÓDIGO DE RECOLECCIÓN (6 dígitos)
   • Se envía al rider Y al restaurante
   • Restaurante: notificación insistente hasta confirmar preparación
   │
   ▼
PAGO (al recibir)
   • Efectivo con vuelto / transferencia (referencia visible)
   • Compra: total = productos + tarifa delivery
   • Encargo: tarifa delivery sola
   • El rider pide al cliente el código de 4 dígitos de ENTREGA
     (distinto del de recolección) → marca DELIVERED
```

---

## 2. Modelo de datos (nuevas entidades)

### `Order` (reemplaza a DeliveryRequest como centro del universo; DeliveryRequest queda deprecada)
```
Id, TenantId (company), CustomerTokenId
Type: Restaurant | Compra | Encargo
Status: (ver máquina de estados §3)
OriginLocation (geometry 4326), OriginAddress, OriginRefId? (restaurantId o null)
DestinationLocation (geometry), DestinationAddress
DestinationLinkRaw (el link de Maps crudo que pegó)
Description (encargo) — CustomerName, CustomerPhone
PaymentMethod: Cash | Transfer, CashGiven?, TotalAmount (decimal), FeeAmount
PickupCode (string 6), DeliveryCode (string 4)
AssignedRiderId?, OfferedRiderIds (jsonb), OfferCount, ExpiresAt?
```

### `OrderItem` (líneas del pedido — solo Type=Restaurant o Compra)
```
OrderId, MenuItemId?, Name, Quantity, UnitPrice, Notes
```

### `CustomerToken` (acceso sin login)
```
Id, TenantId, Token (GUID aleatorio, URL-safe), Phone, Name?, CreatedBy (company admin), ExpiresAt, IsActive
```

### `OrderEvent` (audit + fuente de notificaciones)
```
OrderId, Timestamp, ActorType (System|Rider|Restaurant|Customer), Description
```

**Decisión:** el link que crea la company lleva `https://app/pedido/{token}`. El token
es la identidad del cliente en esta fase (sin OTP todavía). Un mismo token puede hacer N pedidos.

---

## 3. Máquina de estados de Order

```
Draft ──(customer envía)──▶ WaitingRider
WaitingRider ──(rider acepta)──▶ RiderAccepted
WaitingRider ──(timeout 3 min, sin riders)──▶ OnHold        ──(company re-intenta)──▶ WaitingRider
RiderAccepted ──(restaurante confirma listo / rider en origen para encargo)──▶ ReadyForPickup
ReadyForPickup ──(rider entrega PickupCode en origen)──▶ PickedUp
PickedUp ──▶ InTransit ──(rider entrega DeliveryCode al cliente)──▶ Delivered
cualquiera antes de PickedUp ──(rider rechaza)──▶ WaitingRider (se reoferta al siguiente)
cualquiera ──(customer cancela)──▶ Cancelled
```

---

## 4. Motor de asignación (lo más crítico)

**Servicio:** `OrderAssignmentEngine : BackgroundService` (reemplaza la lógica manual de AssignmentsController)

```
1. Order pasa a WaitingRider
2. Query: riders del tenant, IsOnline, !IsBusy, CurrentLocation NOT NULL
   ORDER BY CurrentLocation <-> origen LIMIT 5   (PostGIS KNN — ya probamos el patrón)
3. Excluir OfferedRiderIds ya notificados
4. El más cercano: crear RiderOffer (Pending), IsBusy=true provisional,
   push FCM + SignalR nuevoAssignment, timer 3 min
5. Acepta → vincular rider a Order, generar códigos, estados
   Rechaza/timeout → RiderOffer=Expired, liberar rider, siguiente candidato
6. Se acabaron candidatos → OnHold + notificar SignalR al tablero company
```

**Reglas anti-abuso:**
- `IsBusy` se marca al OFERTAR (no al aceptar) para no ofertar el mismo rider a 2 órdenes
- El rider puede tener 2 órdenes simultáneas del mismo origin→destination (flag `AcceptsGrouping=false` en MVP)
- Cleanup a los 5 min mata ofertas huérfanas (ya existe RiderCleanupService — ampliar)

**Notificación insistente:** Job dentro del mismo engine reenvía push cada 30s mientras
RiderOffer=Pending y `OfferCount < 6`. El restaurante recibe el mismo trato en RiderAccepted
(confirme listo). Sin FCM configurado aún, SignalR+popup de la app es el fallback (ya funciona).

---

## 5. APIs nuevas

### Público (customer, auth por token en header `X-Customer-Token`)
```
GET  /api/p/{token}/catalog              → restaurantes + menús del tenant
GET  /api/p/{token}/restaurants/{id}     → menú completo
POST /api/p/{token}/orders               → crear orden (restaurant | compra | encargo)
GET  /api/p/{token}/orders              → mis pedidos + estado en vivo
POST /api/p/{token}/orders/{id}/cancel
```

### Company
```
POST   /api/customer-tokens              → generar link, devuelve URL lista para compartir
GET    /api/orders?status=...            → tablero (reemplaza delivery-requests)
POST   /api/orders/{id}/retry-assign     → reactivar OnHold
GET    /api/orders/{id}/events           → timeline
```

### Rider (mantiene SignalR, añade verificación de códigos)
```
POST /api/rider/orders/{id}/accept | reject
POST /api/rider/orders/{id}/pickup      → body { code } — verifica contra restaurant confirma
POST /api/rider/orders/{id}/deliver     → body { code } — verifica contra DeliveryCode
PUT  /api/rider/orders/{id}/status      → InTransit
```

### Restaurante
```
GET  /api/restaurant/orders              → pedidos entrantes
POST /api/restaurant/orders/{id}/confirm-ready  → dispara ReadyForPickup (luz verde)
```

---

## 6. Códigos (pickup y entrega)

| Código | Largo | Quién lo tiene | Quién lo verifica |
|--------|-------|----------------|-------------------|
| PickupCode | 6 dígitos | Rider + Restaurante | Restaurante al entregar la mercancía |
| DeliveryCode | 4 dígitos | Rider + Cliente (vía página del pedido) | Rider en app al entregar |

- Generados con `RandomNumberGenerator` (no `Random`), únicos por orden activo
- 3 intentos fallidos de pickup → Order vuelve a WaitingRider con flag sospechoso
- En el MVP con efectivo: el rider confirma cobro con toggle + monto recibido

---

## 7. Pantallas

### PWA actual (restaurant-pwa) → renombrar a **panel company**
- `/p/{token}` — página pública del cliente (catálogo tipo marketplace, mobile-first, sin login)
- `/order-tracking` — estado en vivo del pedido (rider en mapa con GPS que ya reporta, código de entrega visible)
- Tablero company: órdenes con badges de estado, cola OnHold, botón re-intentar
- Eliminar: flujo "restaurantes piden delivery" (se deprecia DeliveryRequest; la migración la limpia)

### App rider (Flutter)
- Pantalla oferta: tipo de pedido (🍔 restaurante / 🛒 compra / 📦 encargo), origen→destino, distancia, tarifa, **artículos con precios** (para que la compra la cuadre con lo que paga el cliente), cuenta 3 min (ya existe el timer 15s → subir configurable)
- Flujo: Aceptar → ir a origen → **pantalla código** → ingresar PickupCode → PickedUp → navegar destino → DeliveryCode → Delivered

### Restaurante
- En MVP: el restaurante también usa el panel con rol RestaurantAdmin pero ve `restaurant/orders` con popup insistente "Confirma pedido #X"

---

## 8. Fases de implementación (orden de mérito)

| # | Entregable | Modelo IA | Riesgo | Estimación |
|---|-----------|-----------|--------|-----------|
| F2.1 | Modelo de datos: Order, OrderItem, CustomerToken, OrderEvent + migración (sin tocar DeliveryRequest aún) | Kimi K2.7 | Bajo | 1 sesión |
| F2.2 | Endpoints públicos `/api/p/{token}/*` + validación de token + crear orden | Kimi K2.7 | Bajo | 1-2 sesiones |
| F2.3 | **Engine de asignación automática** (KNN + ofertas + timeout + reoferta) — núcleo del cambio | Kimi K2.7 | **Alto** | 2-3 sesiones |
| F2.4 | Códigos pickup/deliver + transiciones verificados | Kimi K2.7 | Medio | 1 sesión |
| F2.5 | Página pública del cliente (catálogo + pedido + tracking) — **es la cara del producto** | Qwen3.7 Plus | Medio | 2-3 sesiones |
| F2.6 | App rider: nuevos tipos de orden + pantalla de códigos | Qwen3.7 Plus | Medio | 2 sesiones |
| F2.7 | Notificaciones push FCM reales (reemplazar fallback SignalR) | Kimi K2.7 | Bajo* | 1-2 sesiones |
| F2.8 | Tests: máquina de estados + engine (simular clocks) + contratos | Kimi K3 | — | 2 sesiones |
| F2.9 | Migración de datos DeliveryRequest→Order + deprecación + limpieza | Kimi K2.7 | Bajo | 1 sesión |

*FCM: requiere cuenta Firebase + API keys — decisión de negocio, no técnica.

**Criterio para F2.3:** el engine es puro estado + relojes → se escribe con
`TimeProvider` inyectable desde el día 1 para poder testear timeouts sin esperar 3 minutos reales.

---

## 9. Decisiones pendientes (necesito tu respuesta antes de F2.5)

1. **Pago:** MVP solo contra entrega (efectivo/transferencia). ¿O quieres pasarela (Stripe/MercadoPago) ya?
2. **Tarifa:** ¿la fija la company por km (`base + km*T` configurable en tenant) o la acuerdan offline?
3. **Compras:** el rider pone dinero de su bolsillo → riesgo. ¿Límite de monto por orden de compra (ej. $30) o flag de "requiere fondo"?
4. **Riders multi-company:** ¿un rider puede estar en 2 companies? (MVP: no — simplifica KNN y IsBusy)
5. **Identidad del cliente:** token en link (sin verificación) ¿suficiente para lanzar, o OTP por SMS antes del público real?

## 10. No-scope de Fase 2 (explícito)

- Pasarela de pago en línea, repartidores de otras companies (agregador), multi-parada (batch delivery), chat cliente↔rider, historial/geocercas por rider, app iOS.
