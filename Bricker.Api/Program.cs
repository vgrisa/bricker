using Bricker.Api.Data;
using Bricker.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Bricker.Api.Hubs;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);
var uploadsPath = Path.Combine(builder.Environment.ContentRootPath, "uploads");
Directory.CreateDirectory(uploadsPath);

const string frontEndPolicy = "BrickerWeb";

builder.Services.AddControllers();
builder.Configuration.AddJsonFile("appsettings.Development.local.json", optional: true, reloadOnChange: true);
// Environment variables and command-line values must override local development settings.
builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddCommandLine(args);

var connectionString = builder.Configuration.GetConnectionString("BrickerDb")
    ?? throw new InvalidOperationException("A connection string 'BrickerDb' não foi configurada.");

builder.Services.AddDbContext<BrickerDbContext>(options => options.UseSqlServer(connectionString));
var authentication = builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme);
authentication.AddIdentityCookies();
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    authentication.AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
        options.SignInScheme = IdentityConstants.ExternalScheme;
        options.ClaimActions.MapCustomJson("urn:google:email_verified", user =>
        {
            if (user.TryGetProperty("email_verified", out var openIdValue)) return openIdValue.ToString();
            if (user.TryGetProperty("verified_email", out var legacyValue)) return legacyValue.ToString();
            return null;
        });
        options.ClaimActions.MapJsonKey("urn:google:hosted_domain", "hd");
    });
}

builder.Services.AddIdentityCore<AppUser>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.User.RequireUniqueEmail = true;
})
    .AddEntityFrameworkStores<BrickerDbContext>()
    .AddSignInManager();

builder.Services.AddAuthorization();
builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    options.AddPolicy(frontEndPolicy, policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

var app = builder.Build();

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});
app.UseCors(frontEndPolicy);
app.UseAuthentication();
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var path = context.Request.Path;
        var allowedWhileIncomplete = path.StartsWithSegments("/api/v1/profile") ||
            path.StartsWithSegments("/api/v1/auth/logout") ||
            path.StartsWithSegments("/api/v1/auth/google");
        if (!allowedWhileIncomplete)
        {
            var userManager = context.RequestServices.GetRequiredService<UserManager<AppUser>>();
            var user = await userManager.GetUserAsync(context.User);
            if (user?.RequiresProfileCompletion == true)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { message = "Complete seu perfil antes de continuar." });
                return;
            }
        }
    }
    await next();
});
app.UseAuthorization();
app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BrickerDbContext>();
    db.Database.Migrate();
}

app.Run();
