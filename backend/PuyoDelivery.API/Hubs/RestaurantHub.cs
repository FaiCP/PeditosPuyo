using Microsoft.AspNetCore.SignalR;

namespace PuyoDelivery.API.Hubs;

public class RestaurantHub : Hub
{
    public async Task JoinRestaurant(Guid restaurantId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"restaurant-{restaurantId}");
    }

    public async Task LeaveRestaurant(Guid restaurantId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"restaurant-{restaurantId}");
    }
}
