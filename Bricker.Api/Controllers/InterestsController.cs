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
[Route("api/v1/listings")]
public sealed class InterestsController(BrickerDbContext db, UserManager<AppUser> userManager) : ControllerBase
{
    [HttpPost("{id:guid}/interests")]
    public async Task<IActionResult> Create(Guid id, CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();
        if (string.IsNullOrWhiteSpace(user.WhatsApp)) return BadRequest(new { message = "Informe seu WhatsApp no perfil antes de demonstrar interesse." });
        var listing = await db.Listings.SingleOrDefaultAsync(item => item.Id == id && item.Status == ListingStatus.Active, cancellationToken);
        if (listing is null) return NotFound();
        if (listing.SellerId == user.Id) return BadRequest(new { message = "Você não pode demonstrar interesse no próprio anúncio." });
        if (await db.ListingInterests.AnyAsync(item => item.ListingId == id && item.InterestedUserId == user.Id, cancellationToken)) return Conflict(new { message = "Seu interesse já foi registrado neste anúncio." });
        db.ListingInterests.Add(new ListingInterest { ListingId = id, InterestedUserId = user.Id });
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("mine/interests")]
    public async Task<ActionResult<IReadOnlyCollection<InterestResponse>>> Mine(CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User);
        var interests = await db.ListingInterests.AsNoTracking().Include(item => item.Listing).Include(item => item.InterestedUser)
            .Where(item => item.Listing.SellerId == userId)
            .OrderByDescending(item => item.CreatedAtUtc)
            .Select(item => new InterestResponse(item.Id, item.ListingId, item.Listing.Title, item.InterestedUser.DisplayName, item.InterestedUser.Email!, item.InterestedUser.WhatsApp, item.CreatedAtUtc))
            .ToListAsync(cancellationToken);
        return Ok(interests);
    }

    [HttpGet("interests/sent")]
    public async Task<ActionResult<IReadOnlyCollection<SentInterestResponse>>> Sent(CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User);
        var interests = await db.ListingInterests.AsNoTracking().Include(item => item.Listing)
            .Where(item => item.InterestedUserId == userId)
            .OrderByDescending(item => item.CreatedAtUtc)
            .Select(item => new SentInterestResponse(item.Id, item.ListingId, item.Listing.Title, item.Listing.Status, item.Listing.SellerDisplayName, item.Listing.ImageUrl, item.CreatedAtUtc))
            .ToListAsync(cancellationToken);
        return Ok(interests);
    }
}
