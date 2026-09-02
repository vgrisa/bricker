using Bricker.Api.Models;

namespace Bricker.Api.Contracts;

public sealed record CategoryResponse(Guid Id, string Name, string Slug);

public sealed record ListingResponse(
    Guid Id,
    string Title,
    string Description,
    decimal Price,
    string Unit,
    decimal Quantity,
    MaterialCondition Condition,
    ListingStatus Status,
    string City,
    string State,
    string Category,
    string CategorySlug,
    string SellerDisplayName,
    string? ImageUrl,
    DateTime CreatedAtUtc);

public sealed record PagedResponse<T>(IReadOnlyCollection<T> Items, int Page, int PageSize, int TotalCount);

public sealed record UpsertListingRequest(
    Guid CategoryId,
    string Title,
    string Description,
    decimal Price,
    string Unit,
    decimal Quantity,
    MaterialCondition Condition,
    string City,
    string State,
    List<IFormFile>? Images);

public sealed record ListingImageResponse(Guid Id, string Url, int SortOrder);
public sealed record SellerResponse(string DisplayName, string? City, string? State, DateTime CreatedAtUtc);
public sealed record ListingDetailResponse(ListingResponse Listing, IReadOnlyCollection<ListingImageResponse> Images, SellerResponse? Seller);
public sealed record ReorderImagesRequest(IReadOnlyCollection<Guid> ImageIds);
public sealed record InterestResponse(Guid Id, Guid ListingId, string ListingTitle, string DisplayName, string Email, string? WhatsApp, DateTime CreatedAtUtc);
