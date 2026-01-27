using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using VideoStreamBackend.Models;
using VideoStreamBackend.Services;

namespace VideoStreamBackend.Controllers;

[ApiController]
[Route("{controller}")]
public class HomeController(IRoomService roomService, IRecentRoomService recentRoomService, UserManager<ApplicationUser> userManager) : Controller {
    
    [HttpGet]
    public async Task<IActionResult> Index(bool includeRecentRooms = false) {
        IEnumerable<HomeRoomNameDisplay> names = roomService.GetMultiple<HomeRoomNameDisplay>(x => new HomeRoomNameDisplay {
            RoomId = x.Id,
            RoomName = x.Name
        });

        Home model = new Home {
            RoomNames = names
        };

        if (includeRecentRooms && HttpContext.User.Identity?.IsAuthenticated == true) {
            ApplicationUser? user = await userManager.GetUserAsync(HttpContext.User);
            if (user == null) {
                model.RecentRooms = recentRoomService.GetMultiple<HomeRoomNameDisplay>(recentRoom => new HomeRoomNameDisplay {
                    RoomId = recentRoom.RoomId,
                    RoomName = recentRoom.Room.Name
                }, recentRoom => recentRoom.UserId.Equals(user.Id));
            }
        }

        return Ok();
    }
}