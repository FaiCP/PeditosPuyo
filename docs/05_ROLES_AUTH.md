# Roles y Autenticación

## Roles

| Rol | Descripción | Puede |
|-----|-------------|-------|
| SuperAdmin | Dueño de la plataforma | CRUD empresas, suscripciones, pagos, ver todo |
| CompanyAdmin | Admin de empresa de delivery | CRUD riders, asignar delivery requests, ver métricas |
| RestaurantAdmin | Admin de restaurante | Solicitar delivery, ver estado de sus requests |
| Rider | Motorizado | Ver assignments, aceptar/rechazar, actualizar ubicación |
| Customer | Cliente final | **Fase 2:** ver catálogo, hacer pedidos |

## Autenticación JWT

- Algoritmo: HS256
- Expiración: 24 horas (Fase 1), 7 días (Fase 2 con refresh token)
- Claims:

```json
{
  "sub": "user-guid",
  "tenant_id": "delivery-company-guid",
  "role": "CompanyAdmin",
  "company_id": "delivery-company-guid",
  "restaurant_id": "restaurant-guid",
  "rider_id": "rider-guid",
  "email": "admin@company.com",
  "full_name": "Admin User"
}
```

## Flujo de Login

1. Cliente envía `POST /auth/login` con `{ email, password }`.
2. Backend valida credenciales con Identity.
3. Backend genera JWT con claims según el rol.
4. Devuelve `{ token, user, role, tenantId }`.
5. Cliente guarda token en memoria / Secure Storage (Flutter).
6. Cliente envía `Authorization: Bearer {token}` en cada request.

## Flujo de Registro

### CompanyAdmin (Alta por SuperAdmin)
1. SuperAdmin crea la empresa.
2. SuperAdmin crea el usuario CompanyAdmin.
3. CompanyAdmin recibe credenciales.

### RestaurantAdmin (Fase 1: manual)
1. Restaurante se registra en la PWA (o el SuperAdmin lo crea).
2. Se le asigna un TenantId.

### Rider (Alta por CompanyAdmin)
1. CompanyAdmin crea el rider.
2. Se genera usuario + rider profile.
3. Rider recibe credenciales y descarga el APK.

## Protección por Endpoint

| Rol | Endpoints permitidos |
|-----|---------------------|
| SuperAdmin | `/companies/*`, `/subscriptions/*`, `/auth/*` |
| CompanyAdmin | `/riders/*`, `/delivery-requests/*`, `/assignments/*` |
| RestaurantAdmin | `/restaurants/*`, `/delivery-requests/*` (solo los suyos) |
| Rider | `/driver/location`, `/assignments/rider/*` |

## Multi-tenancy y Autorización

- El `TenantId` del JWT se inyecta automáticamente en cada operación.
- EF Core Global Query Filter asegura que solo ve los datos de su tenant.
- CompanyAdmin no ve riders de otras empresas.
- RestaurantAdmin solo ve sus propios delivery requests.
