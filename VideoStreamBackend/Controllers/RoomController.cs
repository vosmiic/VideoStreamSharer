using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VideoStreamBackend.Identity;
using VideoStreamBackend.Models;

namespace VideoStreamBackend.Controllers;

[ApiController]
[Route("[controller]")]
public class RoomController : Controller {
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public RoomController(ApplicationDbContext context, UserManager<ApplicationUser> userManager) {
        _context = context;
        _userManager = userManager;
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult> CreateRoom([FromBody] string roomName) {
        var user = await _userManager.GetUserAsync(HttpContext.User);
        if (user == null) return Unauthorized();
        Room room = new Room {
            Name = roomName,
            Owner = user
        };
        _context.Rooms.Add(room);
        await _context.SaveChangesAsync();
        return Ok(room.Id);
    }
    
    [HttpGet]
    [Route("{roomId}")]
    public async Task<ActionResult> GetRoom([FromRoute] Guid roomId) {
        var room = await _context.Rooms.Include(room => room.Queue).FirstOrDefaultAsync(room => room.Id == roomId);
        return room != null ? Ok(room) : NotFound();
    }
}