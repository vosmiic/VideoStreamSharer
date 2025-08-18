using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using VideoStreamBackend.Models;
using Stream = VideoStreamBackend.Models.Stream.Stream;

namespace VideoStreamBackend.Controllers;

public class StreamController : Controller {
    public UserManager<ApplicationUser> _userManager { get; set; }
    public IConfiguration _configuration { get; set; }

    public StreamController(UserManager<ApplicationUser> userManager, IConfiguration configuration) {
        _userManager = userManager;
        _configuration = configuration;
    }
    
    [Authorize]
    [Route("stream/{userId}")]
    public async Task<IActionResult> Index(Guid userId) {
        IdentityUser? user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return new NotFoundResult();
        var streamInputUrl = _configuration["Stream:InputUrl"];
        var streamOutputUrl = _configuration["Stream:OutputUrl"];
        if (streamInputUrl == null || streamOutputUrl == null) return new BadRequestResult();

        return Ok(new Stream {
            InputUrl = streamInputUrl.Replace("{user}", user.Id),
            OutputUrl = streamOutputUrl.Replace("{user}", user.Id),
        });
    }
}