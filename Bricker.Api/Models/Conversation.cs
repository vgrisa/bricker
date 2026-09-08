namespace Bricker.Api.Models;

public sealed class Conversation
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ListingInterestId { get; init; }
    public Guid ListingId { get; init; }
    public required string BuyerId { get; init; }
    public required string SellerId { get; init; }
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
    public DateTime? LastMessageAtUtc { get; set; }
    public ListingInterest ListingInterest { get; init; } = null!;
    public Listing Listing { get; init; } = null!;
    public AppUser Buyer { get; init; } = null!;
    public AppUser Seller { get; init; } = null!;
    public ICollection<ChatMessage> Messages { get; init; } = new List<ChatMessage>();
}

public sealed class ChatMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ConversationId { get; init; }
    public required string SenderId { get; init; }
    public required string Body { get; init; }
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
    public DateTime? ReadAtUtc { get; set; }
    public Conversation Conversation { get; init; } = null!;
    public AppUser Sender { get; init; } = null!;
}
