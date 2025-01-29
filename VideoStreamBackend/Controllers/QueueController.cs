using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VideoStreamBackend.Identity;
using VideoStreamBackend.Models.PlayableType;

namespace VideoStreamBackend.Controllers;

[ApiController]
[Route("[controller]")]
public class QueueController : Controller {
    private readonly ApplicationDbContext _context;

    public QueueController(ApplicationDbContext context) {
        _context = context;
    }

    [HttpPost]
    [Route("{roomId}/add")]
    public async Task<ActionResult> AddToQueue([FromRoute] Guid roomId, [FromBody] string url) {
        var room = await _context.Rooms.FirstOrDefaultAsync(room => room.Id == roomId);
        if (room == null) return new NotFoundResult();
        
        Regex regex = new Regex(@"(?:youtube\.com\/(?:[^\/]+\/.+\/|(?:v|e(?:mbed)?)\/|.*[?&]v=)|youtu\.be\/)([^""&?\/\s]{11})", RegexOptions.IgnoreCase);

        MatchCollection matchCollection = regex.Matches(url);
        string? videoId = null;
        foreach (Match match in matchCollection) {
            GroupCollection groupCollection = match.Groups;
            videoId = groupCollection[1].Value;
        }
        
        if (videoId == null) return new BadRequestResult();
        
        room.Queue.Add(new YouTubeVideo {
            Title = "YouTube Video", //todo get title from youtube metadata endpoint
            VideoId = videoId
        });

        await _context.SaveChangesAsync();
        return Ok();
    }
}