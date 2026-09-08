using Bricker.Api.Contracts;
using Bricker.Api.Data;
using Bricker.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Bricker.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/listings")]
public sealed class InterestsController(BrickerDbContext db, UserManager<AppUser> userManager) : ControllerBase
{
    [HttpPost("{id:guid}/interests")]
    public async Task<ActionResult<InterestCreatedResponse>> Create(Guid id, CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();
        var listing = await db.Listings.SingleOrDefaultAsync(item => item.Id == id && item.Status == ListingStatus.Active, cancellationToken);
        if (listing is null) return NotFound();
        if (listing.SellerId == user.Id) return BadRequest(new { message = "Você não pode demonstrar interesse no próprio anúncio." });
        if (string.IsNullOrWhiteSpace(listing.SellerId)) return BadRequest(new { message = "Este anúncio não possui um vendedor disponível para conversa." });

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var interest = await db.ListingInterests.Include(item => item.Conversation)
            .SingleOrDefaultAsync(item => item.ListingId == id && item.InterestedUserId == user.Id, cancellationToken);
        if (interest?.Conversation is not null)
        {
            await transaction.CommitAsync(cancellationToken);
            return Ok(new InterestCreatedResponse(interest.Id, interest.Conversation.Id));
        }

        interest ??= new ListingInterest { ListingId = id, InterestedUserId = user.Id };
        if (db.Entry(interest).State == EntityState.Detached) db.ListingInterests.Add(interest);
        var conversation = new Conversation
        {
            ListingInterestId = interest.Id,
            ListingId = listing.Id,
            BuyerId = user.Id,
            SellerId = listing.SellerId
        };
        db.Conversations.Add(conversation);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok(new InterestCreatedResponse(interest.Id, conversation.Id));
    }

    [HttpGet("mine/interests")]
    public async Task<ActionResult<IReadOnlyCollection<InterestResponse>>> Mine(CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User);
        var interests = await db.ListingInterests.AsNoTracking().Include(item => item.Listing).Include(item => item.InterestedUser)
            .Where(item => item.Listing.SellerId == userId)
            .OrderByDescending(item => item.CreatedAtUtc)
            .Select(item => new InterestResponse(item.Id, item.ListingId, item.Conversation == null ? null : item.Conversation.Id, item.InterestedUserId, item.Listing.Title, item.InterestedUser.DisplayName, item.CreatedAtUtc))
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
            .Select(item => new SentInterestResponse(item.Id, item.ListingId, item.Conversation == null ? null : item.Conversation.Id, item.Listing.Title, item.Listing.Status, item.Listing.SellerDisplayName, item.Listing.ImageUrl, item.CreatedAtUtc))
            .ToListAsync(cancellationToken);
        return Ok(interests);
    }
}
