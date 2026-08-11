using Bricker.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

const string frontEndPolicy = "BrickerWeb";

builder.Services.AddControllers();
builder.Configuration.AddJsonFile("appsettings.Development.local.json", optional: true, reloadOnChange: true);

var connectionString = builder.Configuration.GetConnectionString("BrickerDb")
    ?? throw new InvalidOperationException("A connection string 'BrickerDb' não foi configurada.");

builder.Services.AddDbContext<BrickerDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddCors(options =>
{
    options.AddPolicy(frontEndPolicy, policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

app.UseCors(frontEndPolicy);
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BrickerDbContext>();
    db.Database.Migrate();
}

app.Run();
