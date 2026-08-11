using Bricker.Api.Contracts;
using Bricker.Api.Data;
using Bricker.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bricker.Api.Controllers;

[ApiController]
[Route("api/v1/listings")]
public sealed class ListingsController(BrickerDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<ListingResponse>>> Search(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] string? city,
        [FromQuery] string? state,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = db.Listings.AsNoTracking().Include(listing => listing.Category)
            .Where(listing => listing.Status == ListingStatus.Active);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(listing => listing.Title.Contains(term) || listing.Description.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(category)) query = query.Where(listing => listing.Category.Slug == category.Trim().ToLower());
        if (!string.IsNullOrWhiteSpace(city)) query = query.Where(listing => listing.City == city.Trim());
        if (!string.IsNullOrWhiteSpace(state)) query = query.Where(listing => listing.State == state.Trim().ToUpper());
        if (minPrice is not null) query = query.Where(listing => listing.Price >= minPrice);
        if (maxPrice is not null) query = query.Where(listing => listing.Price <= maxPrice);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(listing => listing.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(listing => ToResponse(listing))
            .ToListAsync(cancellationToken);

        return Ok(new PagedResponse<ListingResponse>(items, page, pageSize, totalCount));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ListingResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var listing = await db.Listings.AsNoTracking().Include(item => item.Category)
            .Where(item => item.Id == id && item.Status == ListingStatus.Active)
            .Select(item => ToResponse(item))
            .SingleOrDefaultAsync(cancellationToken);

        return listing is null ? NotFound() : Ok(listing);
    }

    private static ListingResponse ToResponse(Listing listing) => new(
        listing.Id, listing.Title, listing.Description, listing.Price, listing.Unit, listing.Quantity,
        listing.Condition, listing.Status, listing.City, listing.State, listing.Category.Name,
        listing.Category.Slug, listing.SellerDisplayName, listing.CreatedAtUtc);
}
