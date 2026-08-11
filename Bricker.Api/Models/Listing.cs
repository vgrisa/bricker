namespace Bricker.Api.Models;

public sealed class Listing
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid CategoryId { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public decimal Price { get; set; }
    public required string Unit { get; set; }
    public decimal Quantity { get; set; }
    public MaterialCondition Condition { get; set; }
    public ListingStatus Status { get; set; } = ListingStatus.Draft;
    public required string City { get; set; }
    public required string State { get; set; }
    public required string SellerDisplayName { get; set; }
    public string? SellerId { get; init; }
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
    public byte[] RowVersion { get; init; } = [];
    public Category Category { get; set; } = null!;
    public AppUser? Seller { get; init; }
}

public enum MaterialCondition { Excellent, Good, Fair }
public enum ListingStatus { Draft, Active, Reserved, Sold, Inactive }
