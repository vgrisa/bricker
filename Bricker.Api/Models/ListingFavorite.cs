namespace Bricker.Api.Models;

public sealed class ListingFavorite
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ListingId { get; init; }
    public required string UserId { get; init; }
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
    public Listing Listing { get; init; } = null!;
    public AppUser User { get; init; } = null!;
}
