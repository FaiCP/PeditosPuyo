using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using PuyoDelivery.API.Hubs;
using PuyoDelivery.Core.Dtos;
using PuyoDelivery.Core.Entities;
using PuyoDelivery.Infrastructure.Data;
using PuyoDelivery.Infrastructure.Services;

namespace PuyoDelivery.API.Background;

/// <summary>
/// Núcleo de la asignación automática de riders y notificación insistente a restaurantes.
/// Es una clase con TimeProvider inyectable para poder testear timeouts sin esperar en real.
/// </summary>
public class AssignmentEngine
{
    public static readonly TimeSpan RiderOfferTimeout = TimeSpan.FromMinutes(2);
    public static readonly TimeSpan RiderNudgeInterval = TimeSpan.FromSeconds(40);
    public const int MaxRiderAttempts = 4;
    public static readonly TimeSpan RestaurantNudgeInterval = TimeSpan.FromSeconds(30);
    public static readonly TimeSpan RestaurantTimeout = TimeSpan.FromMinutes(3);
    public const double MaxRadiusKm = 15.0;

    private readonly ApplicationDbContext _db;
    private readonly IHubContext<RiderHub> _riderHub;
    private readonly IHubContext<RestaurantHub> _restaurantHub;
    private readonly IHubContext<CompanyHub> _companyHub;
    private readonly FcmV1Service _fcm;
    private readonly TimeProvider _clock;

    public AssignmentEngine(
        ApplicationDbContext db,
        IHubContext<RiderHub> riderHub,
        IHubContext<RestaurantHub> restaurantHub,
        IHubContext<CompanyHub> companyHub,
        FcmV1Service fcm,
        TimeProvider clock)
    {
        _db = db;
        _riderHub = riderHub;
        _restaurantHub = restaurantHub;
        _companyHub = companyHub;
        _fcm = fcm;
        _clock = clock;
    }

    // ---- Un tick del motor ----
    public async Task<int> RunTickAsync()
    {
        var now = _clock.GetUtcNow().UtcDateTime;
        var changed = 0;
        changed += await ProcessWaitingRidersAsync(now);
        changed += await ProcessRestaurantNudgesAsync(now);
        if (changed > 0) await _db.SaveChangesAsync();
        return changed;
    }

    // ---- Orders esperando rider: crear oferta, re-notificar, expirar, cancelar ----
    private async Task<int> ProcessWaitingRidersAsync(DateTime now)
    {
        var orders = await _db.Orders
            .IgnoreQueryFilters()
            .Include(o => o.Offers)
            .Where(o => o.Status == OrderStatus.WaitingRider)
            .OrderBy(o => o.SubmittedAt)
            .Take(100)
            .ToListAsync();

        var touched = 0;
        foreach (var order in orders)
        {
            var pending = order.Offers.FirstOrDefault(o => o.Status == RiderOfferStatus.Pending);

            if (pending == null)
            {
                // Sin oferta activa: o se agotaron los intentos (cancelar) o creamos la siguiente.
                if (order.OfferCount >= MaxRiderAttempts)
                {
                    CancelOrder(order, OrderCancelReason.NoRidersAvailable,
                        "No hay servicios de riders disponibles en este momento.");
                    await _companyHub.Clients.Group($"company-{order.TenantId}")
                        .SendAsync("orderNoRiders", new { orderId = order.Id, reason = order.CancelDetail });
                }
                else
                {
                    await CreateOfferAsync(order, now);
                }
                touched++;
            }
            else if (now >= pending.ExpiresAt)
            {
                pending.Status = RiderOfferStatus.Expired;
                pending.RespondedAt = now;
                await ReleaseRiderIfFreeAsync(pending.RiderId, pending.Id, now);
                touched++;
            }
            else if (now - pending.LastNotifiedAt >= RiderNudgeInterval)
            {
                pending.LastNotifiedAt = now;
                pending.NotifyCount++;
                await NotifyRiderOfferAsync(order, pending);
                touched++;
            }
        }
        return touched;
    }

    private async Task CreateOfferAsync(Order order, DateTime now)
    {
        var rejectedIds = order.Offers.Where(o => o.Status == RiderOfferStatus.Rejected).Select(o => o.RiderId).ToHashSet();
        var candidate = await FindNearestAvailableRiderAsync(order, rejectedIds);

        if (candidate == null)
        {
            // No hay ningún rider libre ahora: poner en espera (no consume intento) y avisar a la company.
            order.Status = OrderStatus.OnHold;
            LogEvent(order, OrderActorType.System, "Sin riders disponibles: pedido en espera");
            await _companyHub.Clients.Group($"company-{order.TenantId}")
                .SendAsync("orderOnHold", new { orderId = order.Id });
            return;
        }

        var offer = new RiderOffer
        {
            TenantId = order.TenantId,
            OrderId = order.Id,
            RiderId = candidate.Id,
            Status = RiderOfferStatus.Pending,
            AttemptNumber = order.OfferCount + 1,
            NotifyCount = 0,
            LastNotifiedAt = now,
            ExpiresAt = now.Add(RiderOfferTimeout)
        };
        _db.RiderOffers.Add(offer);
        candidate.IsBusy = true;
        order.OfferCount++;
        LogEvent(order, OrderActorType.System, $"Oferta enviada al rider {candidate.FullName} (intento {offer.AttemptNumber}/{MaxRiderAttempts})");
        await NotifyRiderOfferAsync(order, offer);
    }

    private async Task<Rider?> FindNearestAvailableRiderAsync(Order order, HashSet<Guid> excludeIds)
    {
        var origin = order.OriginLocation;
        var riders = await _db.Riders
            .IgnoreQueryFilters()
            .Where(r => r.TenantId == order.TenantId && r.IsOnline && !r.IsBusy && r.IsActive
                        && r.CurrentLocation != null)
            .ToListAsync();

        return riders
            .Where(r => !excludeIds.Contains(r.Id) && r.CurrentLocation != null)
            .Select(r => (rider: r, km: HaversineKm(origin.Y, origin.X, r.CurrentLocation!.Y, r.CurrentLocation.X)))
            .Where(x => x.km <= MaxRadiusKm)
            .OrderBy(x => x.km)
            .Select(x => x.rider)
            .FirstOrDefault();
    }

    // ---- Notificación insistente al restaurante tras aceptar el rider ----
    private async Task<int> ProcessRestaurantNudgesAsync(DateTime now)
    {
        var orders = await _db.Orders
            .IgnoreQueryFilters()
            .Include(o => o.Restaurant)
            .Where(o => o.Status == OrderStatus.RiderAccepted
                        && o.Type == OrderType.Restaurant
                        && o.RestaurantConfirmedAt == null)
            .Take(100)
            .ToListAsync();

        var touched = 0;
        foreach (var order in orders)
        {
            var acceptedAt = order.RiderAcceptedAt ?? order.CreatedAt;

            // 3 min sin respuesta del restaurante -> cancelar.
            if (now - acceptedAt >= RestaurantTimeout)
            {
                CancelOrder(order, OrderCancelReason.RestaurantNoResponse,
                    "El restaurante no respondió al pedido.");
                await ReleaseRiderIfFreeAsync(order.AssignedRiderId!.Value, Guid.Empty, now);
                await _companyHub.Clients.Group($"company-{order.TenantId}")
                    .SendAsync("orderCancelled", new { orderId = order.Id, reason = order.CancelDetail });
                touched++;
                continue;
            }

            if (order.LastRestaurantNotifyAt == null || now - order.LastRestaurantNotifyAt >= RestaurantNudgeInterval)
            {
                order.LastRestaurantNotifyAt = now;
                order.RestaurantNotifyCount++;
                if (order.RestaurantId.HasValue)
                {
                    await _restaurantHub.Clients.Group($"restaurant-{order.RestaurantId}").SendAsync("confirmOrder", new
                    {
                        orderId = order.Id,
                        origin = order.OriginName,
                        rider = order.AssignedRiderId,
                        notifyCount = order.RestaurantNotifyCount
                    });
                }
                touched++;
            }
        }
        return touched;
    }

    // ---- Helpers ----
    private async Task NotifyRiderOfferAsync(Order order, RiderOffer offer)
    {
        await _riderHub.Clients.Group($"rider-{offer.RiderId}").SendAsync("newOrderOffer", new
        {
            offerId = offer.Id,
            orderId = order.Id,
            type = order.Type.ToString().ToLowerInvariant(),
            originName = order.OriginName,
            originAddress = order.OriginAddress,
            destinationAddress = order.DestinationAddress,
            deliveryFee = order.DeliveryFeeAmount,
            items = order.Items.Select(i => new OrderTrackingItemDto(i.Name, i.Quantity, i.UnitPrice)).ToList(),
            expiresAt = offer.ExpiresAt
        });

        var rider = await _db.Riders.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Id == offer.RiderId);
        if (rider != null)
        {
            var typeLabel = order.Type switch
            {
                OrderType.Restaurant => "Restaurante",
                OrderType.Compra => "Compra",
                OrderType.Encargo => "Encargo",
                _ => "Pedido"
            };
            await _fcm.SendToTokenAsync(rider.FcmToken,
                $"Nuevo {typeLabel.ToLowerInvariant()} disponible",
                $"{order.OriginName} → {order.DestinationAddress} · tarifa ${order.DeliveryFeeAmount:F2}",
                new Dictionary<string, string>
                {
                    ["event"] = "newOrderOffer",
                    ["offerId"] = offer.Id.ToString(),
                    ["orderId"] = order.Id.ToString()
                });
        }
    }

    private async Task ReleaseRiderIfFreeAsync(Guid riderId, Guid excludeOfferId, DateTime now)
    {
        // Liberar al rider salvo que siga con OTRA oferta activa u otra orden en curso.
        var stillBusy = await _db.RiderOffers.AnyAsync(o =>
            o.RiderId == riderId && o.Id != excludeOfferId && o.Status == RiderOfferStatus.Pending);
        if (stillBusy) return;

        var hasActiveOrder = await _db.Orders.AnyAsync(o =>
            o.AssignedRiderId == riderId &&
            (o.Status == OrderStatus.RiderAccepted || o.Status == OrderStatus.ReadyForPickup
             || o.Status == OrderStatus.PickedUp || o.Status == OrderStatus.InTransit));
        if (hasActiveOrder) return;

        var rider = await _db.Riders.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Id == riderId);
        if (rider != null) rider.IsBusy = false;
    }

    private void CancelOrder(Order order, OrderCancelReason reason, string detail)
    {
        order.Status = OrderStatus.Cancelled;
        order.CancelReason = reason;
        order.CancelDetail = detail;
        order.CancelledAt = _clock.GetUtcNow().UtcDateTime;
        LogEvent(order, OrderActorType.System, $"Cancelado: {detail}");
    }

    private void LogEvent(Order order, OrderActorType actor, string description)
    {
        _db.OrderEvents.Add(new OrderEvent
        {
            OrderId = order.Id,
            TenantId = order.TenantId,
            ActorType = actor,
            Description = description
        });
    }

    private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371.0;
        var dLat = (lat2 - lat1) * Math.PI / 180.0;
        var dLon = (lon2 - lon1) * Math.PI / 180.0;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }
}
