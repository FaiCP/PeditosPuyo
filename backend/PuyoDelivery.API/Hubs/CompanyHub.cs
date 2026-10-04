using Microsoft.AspNetCore.SignalR;

namespace PuyoDelivery.API.Hubs;

public class CompanyHub : Hub
{
    public async Task JoinCompany(Guid companyId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"company-{companyId}");
    }

    public async Task LeaveCompany(Guid companyId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"company-{companyId}");
    }
}
