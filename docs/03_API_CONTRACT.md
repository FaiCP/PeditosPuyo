# API Contract

## Base URL (desarrollo): `https://localhost:5001/api`

## Autenticación

Todos los endpoints excepto `POST /auth/login` y `POST /auth/register` requieren header:
```
Authorization: Bearer {jwt_token}
```

### JWT Claims
- `sub` = user_id
- `tenant_id` = DeliveryCompany.Id
- `role` = SuperAdmin | CompanyAdmin | RestaurantAdmin | Rider
- `company_id` = DeliveryCompany.Id (si aplica)
- `restaurant_id` = Restaurant.Id (si aplica)
- `rider_id` = Rider.Id (si aplica)

---

## Auth

| Método | Ruta | Body | Response |
|--------|------|------|----------|
| POST | `/auth/login` | `{ email, password }` | `{ token, user, role, tenantId }` |
| POST | `/auth/register` | `{ fullName, email, phone, password, role }` | `{ token, user, role }` |

---

## Companies (Solo SuperAdmin)

| Método | Ruta | Body | Response |
|--------|------|------|----------|
| GET | `/companies` | — | `[CompanyDto]` |
| POST | `/companies` | `{ name, slug, monthlyRatePerDriver, riderLimit }` | `CompanyDto` |
| GET | `/companies/{id}` | — | `CompanyDto` |
| PUT | `/companies/{id}` | `{ name, slug, monthlyRatePerDriver, riderLimit, isActive }` | `CompanyDto` |
| DELETE | `/companies/{id}` | — | `204` |

---

## Subscriptions (Solo SuperAdmin)

| Método | Ruta | Body | Response |
|--------|------|------|----------|
| GET | `/subscriptions` | — | `[SubscriptionDto]` |
| POST | `/subscriptions` | `{ companyId, planName, pricePerMonth, riderLimit, months }` | `SubscriptionDto` |
| POST | `/subscriptions/{id}/payments` | `{ amount, method, reference }` | `CompanyPaymentDto` |

---

## Riders (CompanyAdmin)

| Método | Ruta | Body | Response |
|--------|------|------|----------|
| GET | `/riders` | — | `[RiderDto]` |
| GET | `/riders/online` | — | `[RiderDto]` |
| POST | `/riders` | `{ fullName, phone, vehiclePlate, email, password }` | `RiderDto` |
| PUT | `/riders/{id}` | `{ fullName, phone, vehiclePlate, isActive }` | `RiderDto` |
| DELETE | `/riders/{id}` | — | `204` |
| POST | `/driver/location` | `{ lat, lng }` | `{ success }` |

---

## Restaurants (Tenant)

| Método | Ruta | Body | Response |
|--------|------|------|----------|
| GET | `/restaurants` | — | `[RestaurantDto]` (público con datos públicos) |
| GET | `/restaurants/{id}` | — | `RestaurantDto` + `[MenuItemDto]` |
| POST | `/restaurants` | `{ name, address, phone, lat, lng, menuSummary }` | `RestaurantDto` |
| PUT | `/restaurants/{id}` | `{ name, address, phone, lat, lng, menuSummary, isActive }` | `RestaurantDto` |
| POST | `/restaurants/import` | CSV/JSON file | `{ imported, errors }` |
| POST | `/restaurants/{id}/menu-items` | `{ name, description, price }` | `MenuItemDto` |

---

## Delivery Requests

| Método | Ruta | Body | Response |
|--------|------|------|----------|
| GET | `/delivery-requests` | — | `[DeliveryRequestDto]` |
| GET | `/delivery-requests/pending` | — | `[DeliveryRequestDto]` (para admin de empresa) |
| POST | `/delivery-requests` | `{ restaurantId, deliveryAddress, deliveryLat, deliveryLng, notes }` | `DeliveryRequestDto` |
| GET | `/delivery-requests/{id}` | — | `DeliveryRequestDto` |
| PUT | `/delivery-requests/{id}/status` | `{ status }` | `DeliveryRequestDto` |

---

## Assignments

| Método | Ruta | Body | Response |
|--------|------|------|----------|
| POST | `/assignments` | `{ requestId, riderId }` | `AssignmentDto` |
| GET | `/assignments/rider/pending` | — | `[AssignmentDto]` (para rider logueado) |
| PUT | `/assignments/{id}/accept` | — | `AssignmentDto` |
| PUT | `/assignments/{id}/reject` | `{ reason }` | `AssignmentDto` |
| PUT | `/assignments/{id}/status` | `{ status }` | `AssignmentDto` |

---

## Nearest Riders (CompanyAdmin)

| Método | Ruta | Query | Response |
|--------|------|-------|----------|
| GET | `/riders/nearest` | `lat, lng, radiusKm` | `[RiderNearbyDto]` |

---

## SignalR Hubs

### `/hubs/company`
- **Grupo:** `company-{tenantId}`
- **Eventos recibidos:**
  - `newDeliveryRequest` → `{ requestId, restaurantName, address, createdAt }`
  - `requestStatusChanged` → `{ requestId, status, riderName }`
  - `riderLocationUpdated` → `{ riderId, lat, lng, requestId }`

### `/hubs/rider`
- **Grupo:** `rider-{riderId}`
- **Eventos recibidos:**
  - `newAssignment` → `{ assignmentId, requestId, restaurantName, pickupAddress, deliveryAddress, distanceKm }`
  - `assignmentCancelled` → `{ assignmentId, reason }`
- **Eventos enviados:**
  - `updateLocation` → `{ lat, lng }`

### `/hubs/restaurant`
- **Grupo:** `restaurant-{restaurantId}`
- **Eventos recibidos:**
  - `requestCreated` → `{ requestId, status }`
  - `riderAssigned` → `{ requestId, riderName, vehiclePlate, etaMinutes }`
  - `riderLocationUpdated` → `{ lat, lng }`
  - `requestDelivered` → `{ requestId }`

---

## Response Patterns

### Éxito
```json
{ "isSuccess": true, "data": { ... } }
```

### Error
```json
{ "isSuccess": false, "error": "Mensaje de error", "code": "RIDER_NOT_FOUND" }
```

### Validación
```json
{ "isSuccess": false, "errors": { "fieldName": ["Error message"] } }
```
