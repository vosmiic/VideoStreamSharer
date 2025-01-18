using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VideoStreamBackend.Models;

namespace VideoStreamBackend.Identity;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser> {
    public DbSet<Room> Rooms { get; set; }
    
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) :
        base(options)
    { }
}