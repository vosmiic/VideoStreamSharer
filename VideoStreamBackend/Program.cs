using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using VideoStreamBackend.Hubs;
using VideoStreamBackend.Identity;
using VideoStreamBackend.Models;
using VideoStreamBackend.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//     .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddDbContext<ApplicationDbContext>(
    options => {
        options.UseLazyLoadingProxies().UseSqlite("Data Source=database.db");
        options.EnableSensitiveDataLogging();
    });
builder.Services.AddAuthorization()
    .AddCookiePolicy(opt => opt.MinimumSameSitePolicy = SameSiteMode.None);

builder.Services.AddIdentityApiEndpoints<ApplicationUser>(config => {
        config.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllers().AddJsonOptions(options => {
    options.JsonSerializerOptions.PropertyNamingPolicy = null;
});
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSignalR();

builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect("localhost"));
builder.Services.AddHttpClient();

builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddScoped<IQueueItemService, QueueItemService>();

var app = builder.Build();

app.MapIdentityApi<ApplicationUser>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
    app.MapOpenApi();
}

app.UseCors(x => x
    .AllowAnyMethod()
    .AllowAnyHeader()
    .SetIsOriginAllowed(origin => true) // allow any origin
    //.WithOrigins("https://localhost:44351")); // Allow only this origin can also have multiple origins separated with comma
    .AllowCredentials()); // allow credentials
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapHub<PrimaryHub>("/hub");
app.MapHub<StreamHub>("/streamHub");

app.Run();