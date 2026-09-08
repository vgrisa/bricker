namespace Bricker.Api.Models;

public sealed class ListingSale
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ListingId { get; init; }
    public Guid ListingInterestId { get; init; }
    public required string BuyerId { get; init; }
    public required string SellerId { get; init; }
    public DateTime ConfirmedAtUtc { get; init; } = DateTime.UtcNow;
    public Listing Listing { get; init; } = null!;
    public ListingInterest ListingInterest { get; init; } = null!;
    public AppUser Buyer { get; init; } = null!;
    public AppUser Seller { get; init; } = null!;
    public ICollection<UserReview> Reviews { get; init; } = new List<UserReview>();
}

public sealed class UserReview
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ListingSaleId { get; init; }
    public required string ReviewerId { get; init; }
    public required string RevieweeId { get; init; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
    public ListingSale ListingSale { get; init; } = null!;
    public AppUser Reviewer { get; init; } = null!;
    public AppUser Reviewee { get; init; } = null!;
}
