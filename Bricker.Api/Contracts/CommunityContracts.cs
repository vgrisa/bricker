using Bricker.Api.Models;

namespace Bricker.Api.Contracts;

public sealed record InterestCreatedResponse(Guid InterestId, Guid ConversationId);

public sealed record ConversationSummaryResponse(
    Guid Id,
    Guid ListingId,
    string ListingTitle,
    string? ListingImageUrl,
    ListingStatus ListingStatus,
    string OtherUserId,
    string OtherUserDisplayName,
    string? LastMessage,
    DateTime? LastMessageAtUtc,
    int UnreadCount);

public sealed record ChatMessageResponse(
    Guid Id,
    Guid ConversationId,
    string SenderId,
    string SenderDisplayName,
    string Body,
    DateTime CreatedAtUtc,
    DateTime? ReadAtUtc);

public sealed record SendMessageRequest(string Body);
public sealed record CompleteSaleRequest(Guid InterestId);
public sealed record SaleResponse(Guid Id, Guid ListingId, string ListingTitle, string BuyerId, string BuyerDisplayName, string SellerId, string SellerDisplayName, DateTime ConfirmedAtUtc);
public sealed record ReviewRequest(int Rating, string? Comment);
public sealed record PendingReviewResponse(Guid SaleId, Guid ListingId, string ListingTitle, string RevieweeId, string RevieweeDisplayName, string RevieweeRole, DateTime ConfirmedAtUtc);
public sealed record UserReviewResponse(Guid Id, Guid SaleId, string ReviewerId, string ReviewerDisplayName, string RevieweeRole, string ListingTitle, int Rating, string? Comment, DateTime CreatedAtUtc, DateTime? UpdatedAtUtc, bool CanEdit);
public sealed record PublicUserProfileResponse(string Id, string DisplayName, string? City, string? State, DateTime CreatedAtUtc, double? Rating, int ReviewCount, IReadOnlyCollection<UserReviewResponse> Reviews, int Page, int PageSize, int TotalCount);
