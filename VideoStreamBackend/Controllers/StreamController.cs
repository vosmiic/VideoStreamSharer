using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using VideoStreamBackend.Helpers;
using VideoStreamBackend.Hubs;
using VideoStreamBackend.Models;
using Stream = VideoStreamBackend.Models.Stream.Stream;

namespace VideoStreamBackend.Controllers;

public class StreamController : Controller {
    public UserManager<ApplicationUser> _userManager { get; set; }
    public IConfiguration _configuration { get; set; }
    public IHubContext<StreamHub> streamHubContext { get; set; }

    public StreamController(UserManager<ApplicationUser> userManager, IConfiguration configuration, IHubContext<StreamHub> streamHubContext) {
        _userManager = userManager;
        _configuration = configuration;
        this.streamHubContext = streamHubContext;
    }
    
    [Route("stream/{userId}")]
    public async Task<IActionResult> Index(Guid userId) {
        IdentityUser? user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return new NotFoundResult();
        Stream stream = new Stream {
            Name = user.UserName
        };
        if (User.Identity?.IsAuthenticated == true && User.FindFirstValue(ClaimTypes.NameIdentifier) == userId.ToString()) {
            var streamInputUrl = _configuration["Stream:InputUrl"];
            if (streamInputUrl == null) return new BadRequestObjectResult("Stream input url not found in server settings.");
            stream.InputUrl = streamInputUrl.Replace("{user}", user.Id);
        }
        
        var streamOutputUrl = _configuration["Stream:OutputUrl"];
        if (streamOutputUrl == null) return new BadRequestObjectResult("Stream output url not found in server settings.");
        stream.OutputUrl = streamOutputUrl.Replace("{user}", user.Id);
        
        return Ok(stream);
    }

    [HttpGet]
    [Route("stream/live")]
    public async Task<IActionResult> Live(string userId) {
        Guid? detectedUserId = StreamHelper.FindUserId(userId);
        if (detectedUserId == null) return new NotFoundResult();
        string stringifiedUserId = detectedUserId.ToString();
        IdentityUser? user = await _userManager.FindByIdAsync(stringifiedUserId);
        if (user == null) return new NotFoundResult();
        await streamHubContext.Clients.Group(stringifiedUserId).SendAsync("IsLive");
        return Ok();
    }
    
    [HttpGet]
    [Route("stream/offline")]
    public async Task<IActionResult> Offline(string userId) {
        Guid? detectedUserId = StreamHelper.FindUserId(userId);
        if (detectedUserId == null) return new NotFoundResult();
        string stringifiedUserId = detectedUserId.ToString();
        IdentityUser? user = await _userManager.FindByIdAsync(stringifiedUserId);
        if (user == null) return new NotFoundResult();
        await streamHubContext.Clients.Group(stringifiedUserId).SendAsync("Offline");
        return Ok();
    }
}