using Microsoft.AspNetCore.SignalR;

namespace VideoStreamBackend.Hubs;

public class BaseHub : Hub{
    private readonly ILogger<BaseHub> _logger;

    public BaseHub(ILogger<BaseHub> logger) {
        _logger = logger;
    }

    protected void LogInformation(string message) => _logger.LogInformation($"[{Context.ConnectionId}] {message}");
    
}