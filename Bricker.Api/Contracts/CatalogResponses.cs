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
    string State);
