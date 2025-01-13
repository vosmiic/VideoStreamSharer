using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VideoStreamBackend.Identity;

namespace VideoStreamBackend.Controllers;

[ApiController]
[Route("[controller]")]
public class RoomController : Controller {
    private readonly ApplicationDbContext _context;

    public RoomController(ApplicationDbContext context) {
        _context = context;
    }

    [HttpPost]
    [Authorize]
    public string CreateRoom([FromBody] string roomName) {
        return roomName;
    }
}