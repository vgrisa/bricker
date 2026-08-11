var builder = WebApplication.CreateBuilder(args);

const string frontEndPolicy = "BrickerWeb";

builder.Services.AddControllers();
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

app.Run();
