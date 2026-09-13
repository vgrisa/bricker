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

public sealed record InterestConversationResponse(
    Guid InterestId,
    Guid ConversationId,
    string Direction,
    DateTime InterestCreatedAtUtc,
    Guid ListingId,
    string ListingTitle,
    string? ListingImageUrl,
    ListingStatus ListingStatus,
    string OtherUserId,
    string OtherUserDisplayName,
    string? LastMessage,
    DateTime? LastMessageAtUtc,
    int UnreadCount,
    InterestReviewContextResponse? Review);

public sealed record InterestReviewContextResponse(
    Guid SaleId,
    string Status,
    string RevieweeId,
    string RevieweeDisplayName,
    string RevieweeRole,
    DateTime ConfirmedAtUtc,
    UserReviewResponse? MyReview);

public sealed record ChatMessageResponse(
    Guid Id,
    Guid ConversationId,
    string? SenderId,
    string? SenderDisplayName,
    string Body,
    DateTime CreatedAtUtc,
    DateTime? ReadAtUtc,
    ChatMessageType Type);

public sealed record SendMessageRequest(string Body);
public sealed record CompleteSaleRequest(Guid InterestId);
public sealed record SaleResponse(Guid Id, Guid ListingId, string ListingTitle, string BuyerId, string BuyerDisplayName, string SellerId, string SellerDisplayName, DateTime ConfirmedAtUtc);
public sealed record ReviewRequest(int Rating, string? Comment);
public sealed record UserReviewResponse(Guid Id, Guid SaleId, string ReviewerId, string ReviewerDisplayName, string RevieweeRole, string ListingTitle, int Rating, string? Comment, DateTime CreatedAtUtc, DateTime? UpdatedAtUtc, bool CanEdit);
public sealed record PublicUserProfileResponse(string Id, string DisplayName, string? City, string? State, DateTime CreatedAtUtc, double? Rating, int ReviewCount, IReadOnlyCollection<UserReviewResponse> Reviews, int Page, int PageSize, int TotalCount);
