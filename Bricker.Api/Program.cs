using Bricker.Api.Data;
using Bricker.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Bricker.Api.Hubs;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Bricker.Api.Storage;
using Bricker.Api.Binding;
using Microsoft.AspNetCore.Mvc.ModelBinding;

var resetDemoData = args.Contains("--reset-demo-data", StringComparer.OrdinalIgnoreCase);
var builder = WebApplication.CreateBuilder(args);
const string frontEndPolicy = "BrickerWeb";

builder.Services.AddControllers(options =>
{
    var index = options.ValueProviderFactories
        .Select((factory, position) => new { factory, position })
        .SingleOrDefault(item => item.factory is FormValueProviderFactory)?.position ?? 0;
    if (options.ValueProviderFactories.ElementAtOrDefault(index) is FormValueProviderFactory)
        options.ValueProviderFactories.RemoveAt(index);
    options.ValueProviderFactories.Insert(index, new InvariantFormValueProviderFactory());
});
if (builder.Environment.IsDevelopment())
    builder.Configuration.AddJsonFile("appsettings.Development.local.json", optional: true, reloadOnChange: true);
// Environment variables and command-line values must override local development settings.
builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddCommandLine(args);

var configuredUploadsPath = builder.Configuration["Storage:UploadsPath"];
var uploadsPath = string.IsNullOrWhiteSpace(configuredUploadsPath)
    ? Path.Combine(builder.Environment.ContentRootPath, "uploads")
    : configuredUploadsPath;
var uploadStorage = new UploadStorage(uploadsPath);
builder.Services.AddSingleton(uploadStorage);

if (!builder.Environment.IsDevelopment())
{
    var configuredKeysPath = builder.Configuration["DataProtection:KeysPath"];
    var keysPath = string.IsNullOrWhiteSpace(configuredKeysPath)
        ? Path.GetFullPath(Path.Combine(uploadStorage.RootPath, "..", "data-protection"))
        : configuredKeysPath;
    Directory.CreateDirectory(keysPath);
    builder.Services.AddDataProtection()
        .SetApplicationName("Bricker")
        .PersistKeysToFileSystem(new DirectoryInfo(keysPath));
}

var connectionString = builder.Configuration.GetConnectionString("BrickerDb")
    ?? throw new InvalidOperationException("A connection string 'BrickerDb' não foi configurada.");

builder.Services.AddDbContext<BrickerDbContext>(options =>
    options.UseSqlServer(connectionString, sqlServer => sqlServer.EnableRetryOnFailure()));
var authentication = builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme);
authentication.AddIdentityCookies();
builder.Services.Configure<CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme, options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
});
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

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseForwardedHeaders();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadStorage.RootPath),
    RequestPath = "/uploads"
});
if (app.Environment.IsDevelopment()) app.UseCors(frontEndPolicy);
app.UseAuthentication();
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var path = context.Request.Path;
        var requiresCompletedProfile = path.StartsWithSegments("/api") || path.StartsWithSegments("/hubs");
        var allowedWhileIncomplete = path.StartsWithSegments("/api/v1/profile") ||
            path.StartsWithSegments("/api/v1/auth/logout") ||
            path.StartsWithSegments("/api/v1/auth/google");
        if (requiresCompletedProfile && !allowedWhileIncomplete)
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
var webRootPath = app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot");
if (!app.Environment.IsDevelopment() && File.Exists(Path.Combine(webRootPath, "index.html")))
    app.MapFallbackToFile("index.html");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BrickerDbContext>();
    if (resetDemoData)
    {
        if (!app.Environment.IsDevelopment())
            throw new InvalidOperationException("A recriação dos dados de demonstração só pode ser executada em desenvolvimento.");

        var connection = db.Database.GetDbConnection();
        Console.WriteLine("Recriando o banco de desenvolvimento '{0}' em '{1}'...", connection.Database, connection.DataSource);
        await db.Database.EnsureDeletedAsync();
    }

    await db.Database.MigrateAsync();
    if (app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("DemoData:Seed"))
        await DevelopmentDataSeeder.SeedAsync(scope.ServiceProvider, app.Environment.ContentRootPath);
}

if (resetDemoData) return;

app.Run();

public partial class Program { }
