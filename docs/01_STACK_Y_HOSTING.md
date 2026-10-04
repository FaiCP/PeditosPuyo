# Stack y Hosting

## Stack Tecnológico

| Capa | Tecnología | Justificación |
|------|-----------|---------------|
| Backend | ASP.NET Core 9 Web API | Ecosistema .NET, alto rendimiento, integración nativa con SignalR y EF Core |
| Real-time | SignalR | WebSockets con reconexiones automáticas y grupos por tenant |
| ORM | Entity Framework Core | Migrations, LINQ, Global Query Filters para multi-tenancy |
| DB | PostgreSQL 15 + PostGIS | PostGIS permite consultas espaciales (asignación por cercanía) |
| Auth | JWT + ASP.NET Core Identity | Roles y claims escalables |
| PWA | React + Vite + Tailwind CSS | Despliegue rápido en Vercel, accesible desde cualquier dispositivo |
| Rider app | Flutter | APK directo, GPS en segundo plano, push nativas |
| Push | Firebase Cloud Messaging | Gratis para volumen de MVP |
| Geocoding | OpenStreetMap Nominatim | Gratis, suficiente para Fase 1 |

## Hosting

| Componente | Servicio | Plan | Nota |
|------------|----------|------|------|
| Backend | Render | Free tier | Cold start ~30s si inactivo 15min |
| PostgreSQL | Render PostgreSQL | Free tier | 1GB storage, suficiente para MVP |
| PWA | Vercel | Free tier | Despliegue continuo desde Git |
| APK Flutter | Descarga directa | — | Link por WhatsApp, sin Play Store |
| Push | Firebase | Free tier | FCM para notificaciones |
| Geocoding | OpenStreetMap | Free | Nominatim API |

## Cold Start en Render

- Si el servicio recibe tráfico cada <15 min, no se duerme.
- Si pasa 15 min sin tráfico, el primer request tarda ~30s.
- **MVP:** manejable si el usuario navega activamente.
- **Producción:** subir a Render paid tier o migrar a AWS.

## Distribución APK

1. Build release: `flutter build apk --release`.
2. El archivo `app-release.apk` se sube a cloud storage.
3. Link de descarga compartido por WhatsApp.
4. El teléfono debe permitir "instalar desde fuentes desconocidas".
5. **Fase 2:** migrar a Play Store ($25 único) o Firebase App Distribution.
