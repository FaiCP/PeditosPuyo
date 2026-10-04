using Microsoft.AspNetCore.SignalR;
using Moq;

namespace PuyoDelivery.Tests.TestHelpers;

public static class MockHub
{
    public static Mock<IHubContext<THub>> Create<THub>() where THub : Hub
    {
        var hubContext = new Mock<IHubContext<THub>>();
        var clients = new Mock<IHubClients>();
        var proxy = new Mock<IClientProxy>();
        var singleProxy = new Mock<ISingleClientProxy>();

        proxy.Setup(p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        singleProxy.Setup(p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        clients.Setup(c => c.All).Returns(proxy.Object);
        clients.Setup(c => c.Clients(It.IsAny<IReadOnlyList<string>>())).Returns(proxy.Object);
        clients.Setup(c => c.AllExcept(It.IsAny<IReadOnlyList<string>>())).Returns(proxy.Object);
        clients.Setup(c => c.Client(It.IsAny<string>())).Returns(singleProxy.Object);
        clients.Setup(c => c.Group(It.IsAny<string>())).Returns(proxy.Object);
        clients.Setup(c => c.Groups(It.IsAny<IReadOnlyList<string>>())).Returns(proxy.Object);
        clients.Setup(c => c.User(It.IsAny<string>())).Returns(singleProxy.Object);
        clients.Setup(c => c.Users(It.IsAny<IReadOnlyList<string>>())).Returns(proxy.Object);

        hubContext.Setup(h => h.Clients).Returns(clients.Object);
        return hubContext;
    }
}
