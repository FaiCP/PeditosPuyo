# Asignación de Modelos IA por Fase - OpenCode Go

## Plan de referencia: OpenCode Go $10/mes

Límites mensuales por modelo: **$15 / $30 / $60** (20% por 5 horas, 50% por semana).

---

## Tabla resumen de modelos relevantes

| Modelo | Input $/1M | Output $/1M | Cap Mensual | Requests/5h | Contexto |
|--------|-----------|-------------|-------------|-------------|----------|
| Kimi K3 | $3.00 | $15.00 | $15 | 110 | 1M |
| Kimi K2.7 Code | $0.95 | $4.00 | $60 | 1,350 | 262K |
| MiniMax M3 | $0.30 | $1.20 | $60 | 3,200 | 1M |
| Qwen3.7 Plus | $0.40 | $1.60 | $60 | 4,300 | 1M |
| Qwen3.8 Max | $2.00 | $6.00 | $15 | 160 | 1M |
| GLM-5.2 | $1.40 | $4.40 | $60 | 880 | 1M |
| GLM-5.3-Flash | $0.15 | $0.50 | $60 | 6,320 | 1M |
| DeepSeek V4 Flash | $0.15 | $0.60 | $30 | 26,000 | 1M |

---

## Asignación FINAL (aprobada por el usuario)

| Fase | Modelo a usar | Input/Output | Cap Mensual | Requests/5h |
|------|---------------|--------------|-------------|-------------|
| 1. Documentación | **GLM-5.3-Flash** | $0.15/$0.50 | $60 | 6,320 |
| 2. Backend .NET | **Kimi K2.7 Code** (único principal) | $0.95/$4.00 | $60 | 1,350 |
| 3. PWA | **Qwen3.7 Plus** | $0.40/$1.60 | $60 | 4,300 |
| 4. Flutter | **Qwen3.7 Plus** | $0.40/$1.60 | $60 | 4,300 |
| 5. Tests | **Por definir** al llegar a la fase | — | — | — |

---

## Estrategia de uso

### Fase 1 — Documentación (GLM-5.3-Flash)
Escribir MDs de especificación, contratos, flujos. Tareas de redacción estructurada.

### Fase 2 — Backend ASP.NET Core 9 (Kimi K2.7 Code)
Controllers, Services, Repositories, SignalR Hubs, JWT, Multi-tenancy, EF Core, Background Services.

### Fase 3 — PWA (Qwen3.7 Plus)
Componentes React, integración WebSocket, Web Audio API, Tailwind CSS, hooks de estado.

### Fase 4 — Flutter (Qwen3.7 Plus)
pubspec.yaml, background_service, FCM, modal de orden, widgets UI.

### Fase 5 — Tests (Por definir)
El usuario decide el modelo al llegar a esta fase, según tokens usados y contexto acumulado.

---

## Presupuesto estimado (Go $10/mes)

| Modelo | Cap mensual | Uso estimado |
|--------|-------------|--------------|
| GLM-5.3-Flash | $60 | ~2% |
| Kimi K2.7 Code | $60 | ~35% |
| Qwen3.7 Plus | $60 | ~20% |
| Tests | Pendiente | ~10-15% (aprox.) |

**Alternativa recomendada:** si el presupuesto lo permite, **Go Plus ($40/mes)** triplica los límites.
