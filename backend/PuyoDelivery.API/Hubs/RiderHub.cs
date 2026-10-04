using Microsoft.AspNetCore.SignalR;

namespace PuyoDelivery.API.Hubs;

public class RiderHub : Hub
{
    public async Task JoinRider(Guid riderId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"rider-{riderId}");
    }

    public async Task UpdateLocation(Guid riderId, double lat, double lng)
    {
        await Clients.Group($"rider-{riderId}").SendAsync("locationUpdated", new { lat, lng, timestamp = DateTime.UtcNow });
    }
}
