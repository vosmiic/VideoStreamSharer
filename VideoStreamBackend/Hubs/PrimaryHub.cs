using Microsoft.AspNetCore.SignalR;

namespace VideoStreamBackend.Hubs;

public class PrimaryHub : Hub {
    public async Task SendMessage(string message) =>
        await Clients.Client(Context.ConnectionId).SendAsync("ReceiveMessage", Context.ConnectionId);
}