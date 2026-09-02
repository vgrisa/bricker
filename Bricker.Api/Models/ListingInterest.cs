namespace Bricker.Api.Models;

public sealed class ListingInterest
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ListingId { get; init; }
    public required string InterestedUserId { get; init; }
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
    public Listing Listing { get; init; } = null!;
    public AppUser InterestedUser { get; init; } = null!;
}
