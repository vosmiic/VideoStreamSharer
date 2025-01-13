using Microsoft.AspNetCore.Identity;

namespace VideoStreamBackend.Models;

public class Room : GuidPrimaryKey {
    public virtual IdentityUser Owner { get; set; }
    public string UserId { get; set; }
    public string Name { get; set; }
}