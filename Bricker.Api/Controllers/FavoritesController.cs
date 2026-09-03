using Bricker.Api.Contracts;
using Bricker.Api.Data;
using Bricker.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bricker.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/favorites")]
public sealed class FavoritesController(BrickerDbContext db, UserManager<AppUser> userManager) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<ListingResponse>>> Get(CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User);
        var listings = await db.ListingFavorites.AsNoTracking()
            .Where(favorite => favorite.UserId == userId && favorite.Listing.Status == ListingStatus.Active)
            .Include(favorite => favorite.Listing.Category)
            .Include(favorite => favorite.Listing.Images)
            .OrderByDescending(favorite => favorite.CreatedAtUtc)
            .Select(favorite => favorite.Listing)
            .ToListAsync(cancellationToken);
        return Ok(listings.Select(ToResponse).ToList());
    }

    [HttpPost("{listingId:guid}")]
    public async Task<IActionResult> Add(Guid listingId, CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User)!;
        if (!await db.Listings.AnyAsync(item => item.Id == listingId && item.Status == ListingStatus.Active, cancellationToken)) return NotFound();
        if (await db.ListingFavorites.AnyAsync(item => item.ListingId == listingId && item.UserId == userId, cancellationToken)) return NoContent();
        db.ListingFavorites.Add(new ListingFavorite { ListingId = listingId, UserId = userId });
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{listingId:guid}")]
    public async Task<IActionResult> Remove(Guid listingId, CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User);
        var favorite = await db.ListingFavorites.SingleOrDefaultAsync(item => item.ListingId == listingId && item.UserId == userId, cancellationToken);
        if (favorite is null) return NoContent();
        db.ListingFavorites.Remove(favorite);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static ListingResponse ToResponse(Listing listing) => new(
        listing.Id, listing.Title, listing.Description, listing.Price, listing.Unit, listing.Quantity,
        listing.Condition, listing.Status, listing.City, listing.State, listing.PostalCode,
        listing.Street, listing.Neighborhood, listing.AddressNumber, listing.AddressComplement, listing.Category.Name,
        listing.Category.Slug, listing.SellerDisplayName, listing.ImageUrl,
        listing.Images.OrderBy(image => image.SortOrder).Select(image => image.Url).ToList(), listing.CreatedAtUtc);
}
