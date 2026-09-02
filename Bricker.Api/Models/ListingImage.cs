namespace Bricker.Api.Models;

public sealed class ListingImage
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ListingId { get; init; }
    public required string Url { get; set; }
    public int SortOrder { get; set; }
    public Listing Listing { get; init; } = null!;
}
