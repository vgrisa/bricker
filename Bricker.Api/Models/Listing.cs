namespace Bricker.Api.Models;

public sealed class Listing
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid CategoryId { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public decimal Price { get; init; }
    public required string Unit { get; init; }
    public decimal Quantity { get; init; }
    public MaterialCondition Condition { get; init; }
    public ListingStatus Status { get; init; } = ListingStatus.Draft;
    public required string City { get; init; }
    public required string State { get; init; }
    public required string SellerDisplayName { get; init; }
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
    public byte[] RowVersion { get; init; } = [];
    public Category Category { get; init; } = null!;
}

public enum MaterialCondition { Excellent, Good, Fair }
public enum ListingStatus { Draft, Active, Reserved, Sold, Inactive }
