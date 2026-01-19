using Microsoft.AspNetCore.Mvc;
using VideoStreamBackend.Models;
using VideoStreamBackend.Services;

namespace VideoStreamBackend.Controllers;

[ApiController]
[Route("{controller}")]
public class HomeController(IRoomService roomService) : Controller {
    
    [HttpGet]
    public IActionResult Index() {
        IEnumerable<HomeRoomNameDisplay> names = roomService.GetMultiple<HomeRoomNameDisplay>(x => new HomeRoomNameDisplay {
            RoomId = x.Id,
            RoomName = x.Name
        });

        return Ok(new Home {
            RoomNames = names
        });
    }
}