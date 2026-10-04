# Task Backend - ASP.NET Core 9

## Modelo: Kimi K2.7 Code (principal)

## Estructura de Solución

```
backend/
├── PuyoDelivery.sln
├── PuyoDelivery.API/           # Web API, Controllers, Hubs, Middleware
├── PuyoDelivery.Core/          # Entidades, DTOs, Interfaces, Reglas de negocio
├── PuyoDelivery.Infrastructure/ # EF Core, Repositorios, Servicios externos, JWT
└── PuyoDelivery.Tests/         # Unit tests + Integration tests
```

## Dependencias NuGet

### PuyoDelivery.API
- Microsoft.AspNetCore.Authentication.JwtBearer
- Microsoft.AspNetCore.SignalR
- Swashbuckle.AspNetCore (Swagger)

### PuyoDelivery.Infrastructure
- Npgsql.EntityFrameworkCore.PostgreSQL
- Npgsql.EntityFrameworkCore.PostgreSQL.NetTopologySuite (PostGIS)
- Microsoft.AspNetCore.Identity.EntityFrameworkCore
- FirebaseAdmin (FCM)
- BCrypt.Net-Next

## Tareas Detalladas

### 1. Proyecto Core

**Entidades:**
- `BaseEntity` (Id, TenantId, CreatedAt, UpdatedAt)
- `DeliveryCompany`, `Subscription`, `CompanyPayment`
- `CompanyAdmin`, `Rider`, `RestaurantAdmin`
- `Restaurant`, `MenuItem`
- `DeliveryRequest`, `OrderAssignment`

**Interfaces:**
- `IRepository<T>`, `IUnitOfWork`
- `ICurrentTenantService`
- `ISignalRNotifier` (o usar Hub directamente)

**DTOs:**
- `Result<T>`, `Result`
- `LoginRequest`, `LoginResponse`
- `CompanyDto`, `RiderDto`, `RestaurantDto`, `DeliveryRequestDto`, `AssignmentDto`

### 2. Proyecto Infrastructure

**Configuración EF Core:**
- `ApplicationDbContext` con Global Query Filters por TenantId
- `IEntityTypeConfiguration<T>` para cada entidad
- `OnModelCreating` con índices GIST para PostGIS
- Migrations iniciales

**Repositorios:**
- `Repository<T>`, `UnitOfWork`

**Servicios:**
- `CurrentTenantService` (lee tenant_id del JWT)
- `JwtTokenGenerator`
- `FcmNotificationService`
- `GeocodingService` (OpenStreetMap Nominatim)

**Stored Procedures:**
- `get_nearest_drivers(tenant_id, lat, lng, radius_km)`

### 3. Proyecto API

**Controllers:**
- `AuthController` (login, register)
- `CompaniesController` (CRUD, solo SuperAdmin)
- `SubscriptionsController` (CRUD + pagos)
- `RidersController` (CRUD + location + nearest)
- `RestaurantsController` (CRUD + menu + import)
- `DeliveryRequestsController` (CRUD + status)
- `AssignmentsController` (create + accept/reject/status)

**SignalR Hubs:**
- `CompanyHub` (grupo: company-{tenantId})
- `RiderHub` (grupo: rider-{riderId})
- `RestaurantHub` (grupo: restaurant-{restaurantId})

**Middleware:**
- Exception handling middleware
- Tenant resolution middleware
- CORS (permitir origen de Vercel)

**Background Services:**
- `RiderCleanupService` (desconectar riders offline después de 5 min)

### 4. Proyecto Tests

- Unit tests de Services
- Integration tests de controllers (con test database)
- Tests de SignalR

## Orden de Implementación

1. Crear solución y proyectos.
2. Core: entidades + interfaces + DTOs.
3. Infrastructure: DbContext + configuraciones + migraciones.
4. Infrastructure: repositorios + servicios JWT + tenant service.
5. API: AuthController + JWT setup.
6. API: CompaniesController + SubscriptionsController.
7. API: RidersController + location + nearest drivers.
8. API: RestaurantsController + import.
9. API: DeliveryRequestsController + AssignmentsController.
10. API: SignalR Hubs.
11. Infrastructure: Background Service.
12. Tests.

## Comandos Clave

```bash
# Crear proyectos
dotnet new sln -n PuyoDelivery
dotnet new classlib -n PuyoDelivery.Core
dotnet new classlib -n PuyoDelivery.Infrastructure
dotnet new webapi -n PuyoDelivery.API
dotnet new xunit -n PuyoDelivery.Tests
dotnet sln add **/*.csproj

# Migraciones
dotnet ef migrations add InitialCreate --project PuyoDelivery.Infrastructure --startup-project PuyoDelivery.API
dotnet ef database update --project PuyoDelivery.Infrastructure --startup-project PuyoDelivery.API

# Ejecutar
dotnet run --project PuyoDelivery.API

# Tests
dotnet test
```
