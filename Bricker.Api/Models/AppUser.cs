using Microsoft.AspNetCore.Identity;

namespace Bricker.Api.Models;

public sealed class AppUser : IdentityUser
{
    public required string DisplayName { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
}
