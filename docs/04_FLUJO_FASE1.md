# Flujo Fase 1 - Paso a Paso

## Diagrama del Flujo

```
┌──────────┐    llama    ┌──────────┐   pide delivery  ┌──────────┐
│ CLIENTE  │───────────▶│RESTAURANT │────────────────▶│ PLATAFORMA│
└──────────┘             └──────────┘   (PWA Admin)    └──────────┘
                                                           │
                                                           │ asigna
                                                           ▼
                                                    ┌──────────┐
                                                    │ COMPANY  │
                                                    │  ADMIN   │
                                                    └──────────┘
                                                           │
                                                           │ elige rider
                                                           ▼
                                                    ┌──────────┐
                                                    │  RIDER   │
                                                    └──────────┘
```

## Paso 1 - Cliente descubre el restaurante

- Abre la PWA pública (sin login).
- Ve el listado de restaurantes con: nombre, dirección, teléfono, menú resumido.
- **No hace pedido en la plataforma.**
- Llama o WhatsAppea al restaurante directamente.
- Confirma el pedido con el restaurante.

## Paso 2 - Restaurante solicita delivery

- Ingresa al panel privado de la PWA con su cuenta.
- Selecciona "Nuevo Delivery".
- Completa: dirección de entrega, coordenadas (GPS o mapa), notas.
- Sistema crea `DeliveryRequest` con status `PENDING`.
- SignalR notifica a la empresa de delivery asignada.

## Paso 3 - Empresa de delivery recibe el request

- Admin de empresa ve el request en su dashboard.
- Opción A: Ve los 3 riders más cercanos y disponibles (PostGIS).
- Opción B: Busca riders online manualmente.
- Selecciona un rider.
- Sistema crea `OrderAssignment` con status `PENDING`.

## Paso 4 - Rider acepta la carrera

- Recibe notificación push + sonido en Flutter.
- BottomSheet muestra: restaurante, dirección recolección, dirección entrega, distancia.
- Temporizador de 15 segundos.
- **Aceptar:** status → `ACCEPTED`. Se une al grupo SignalR del request.
- **Rechazar:** status → `REJECTED`. Admin puede asignar otro rider.

## Paso 5 - Rider recoge y entrega

- En ruta a recolección: envía GPS cada 5 segundos.
- Al llegar: cambia status a `IN_TRANSIT` (opcional: confirmar en la app).
- En ruta de entrega: sigue enviando GPS.
- Al entregar: cambia status a `DELIVERED`.
- Restaurant y admin ven actualizaciones en tiempo real.

## Paso 6 - Cierre

- Sistema registra tiempo total del delivery.
- Restaurant ve confirmación de entrega.
- El delivery queda en historial para métricas.

---

## Máquina de Estados

### DeliveryRequest
```
PENDING → ASSIGNED → ACCEPTED → IN_TRANSIT → DELIVERED
   │           │          │
   │           │          └─→ REJECTED (rider rechaza)
   │           └─→ CANCELLED (restaurant cancela)
   └─→ CANCELLED
```

### Assignment
```
PENDING → ACCEPTED → IN_TRANSIT → DELIVERED
   │
   └─→ REJECTED
```
