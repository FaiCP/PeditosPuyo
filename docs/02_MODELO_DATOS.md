# Modelo de Datos

## Multi-tenancy

Todas las entidades de negocio heredan `BaseEntity` con `TenantId`.
Filtro global en EF Core: `HasQueryFilter(r => r.TenantId == _currentTenant.Id)`.

## Diagrama de Entidades

### Plataforma (sin TenantId)

```
PlatformUser
  - Id, Email, PasswordHash, Role, IsActive
```

### Tenant (Empresa de Delivery)

```
DeliveryCompany
  - Id, TenantId, Name, Slug, IsActive, CreatedAt

Subscription
  - Id, TenantId, CompanyId, PlanName, PricePerMonth, RiderLimit, Status, ExpiresAt

CompanyPayment
  - Id, TenantId, CompanyId, Amount, Method (TRANSFER/TICKET), Reference, PaidAt, Status
```

### Usuarios del Tenant

```
CompanyAdmin
  - Id, TenantId, UserId, CompanyId

Rider
  - Id, TenantId, CompanyId, UserId, FullName, Phone, VehiclePlate,
    IsOnline, IsBusy, FcmToken, CurrentLocation (GEOGRAPHY), LastLocationUpdate

RestaurantAdmin
  - Id, TenantId, UserId, RestaurantId
```

### Negocios

```
Restaurant
  - Id, TenantId, Name, Slug, Address, Phone, Location (GEOGRAPHY),
    MenuSummary, IsActive, Source (OSM/MANUAL/SCRAPER), ExternalId

MenuItem
  - Id, TenantId, RestaurantId, Name, Description, Price, IsActive
```

### Pedidos / Delivery

```
DeliveryRequest
  - Id, TenantId, RestaurantId, RequestedBy (RestaurantAdminId),
    Status (PENDING/ASSIGNED/ACCEPTED/IN_TRANSIT/DELIVERED/CANCELLED),
    DeliveryAddress, DeliveryLocation (GEOGRAPHY), Notes,
    CreatedAt, AssignedAt, AcceptedAt, DeliveredAt

OrderAssignment
  - Id, TenantId, RequestId, RiderId, AssignedBy (CompanyAdminId),
    Status (PENDING/ACCEPTED/REJECTED/IN_TRANSIT/DELIVERED),
    AssignedAt, AcceptedAt, RejectedAt, RejectionReason, DeliveredAt
```

### Fase 2 (preparado, no implementado aún)

```
Customer
  - Id, TenantId, FullName, Phone, Email

Order
  - Id, TenantId, CustomerId, RestaurantId, RequestId, RiderId,
    Status, PaymentMethod, Subtotal, DeliveryFee, Total,
    DeliveryAddress, DeliveryLocation, CreatedAt

CreditBalance
  - Id, TenantId, UserId, Balance, UpdatedAt

Promotion
  - Id, TenantId, Title, Description, CreditCost, IsActive, ValidUntil
```

## Índices Geoespaciales

```sql
CREATE INDEX idx_rider_location ON riders USING GIST (current_location);
CREATE INDEX idx_restaurant_location ON restaurants USING GIST (location);
CREATE INDEX idx_request_location ON delivery_requests USING GIST (delivery_location);
```

## Stored Procedure: get_nearest_riders

```sql
CREATE OR REPLACE FUNCTION get_nearest_drivers(
  p_tenant_id UUID,
  p_rest_lat DOUBLE PRECISION,
  p_rest_lng DOUBLE PRECISION,
  p_radius_km DOUBLE PRECISION DEFAULT 10.0
)
RETURNS TABLE (
  rider_id UUID,
  full_name VARCHAR(100),
  vehicle_plate VARCHAR(10),
  distance_km DOUBLE PRECISION,
  current_lat DOUBLE PRECISION,
  current_lng DOUBLE PRECISION
)
LANGUAGE plpgsql AS $$
BEGIN
  RETURN QUERY
  SELECT
    r.id,
    r.full_name,
    r.vehicle_plate,
    ST_Distance(
      r.current_location::geography,
      ST_SetSRID(ST_MakePoint(p_rest_lng, p_rest_lat), 4326)::geography
    ) / 1000 AS distance_km,
    ST_Y(r.current_location::geometry) AS current_lat,
    ST_X(r.current_location::geometry) AS current_lng
  FROM riders r
  WHERE r.tenant_id = p_tenant_id
    AND r.is_online = true
    AND r.is_busy = false
    AND r.current_location IS NOT NULL
    AND ST_DWithin(
      r.current_location::geography,
      ST_SetSRID(ST_MakePoint(p_rest_lng, p_rest_lat), 4326)::geography,
      p_radius_km * 1000
    )
  ORDER BY r.current_location <-> ST_SetSRID(ST_MakePoint(p_rest_lng, p_rest_lat), 4326)::geography
  LIMIT 3;
END;
$$;
```
