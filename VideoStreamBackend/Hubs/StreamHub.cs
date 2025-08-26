using Microsoft.AspNetCore.SignalR;

namespace VideoStreamBackend.Hubs;

public class StreamHub : Hub {
    public override Task OnConnectedAsync() {
        var userId = Context.GetHttpContext().Request.Query["userId"];
        Groups.AddToGroupAsync(Context.ConnectionId, userId);
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception) {
        var userId = Context.GetHttpContext().Request.Query["userId"];
        Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
        return base.OnDisconnectedAsync(exception);
    }
    
    
}