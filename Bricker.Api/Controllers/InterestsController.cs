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

[ApiController]
[Authorize]
[Route("api/v1/interests")]
public sealed class InterestCenterController(BrickerDbContext db, UserManager<AppUser> userManager) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<InterestConversationResponse>>> List(CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User)!;
        var rows = await db.Conversations.AsNoTracking()
            .Where(conversation => conversation.BuyerId == userId || conversation.SellerId == userId)
            .Select(conversation => new
            {
                InterestId = conversation.ListingInterestId,
                ConversationId = conversation.Id,
                Direction = conversation.BuyerId == userId ? "sent" : "received",
                InterestCreatedAtUtc = conversation.ListingInterest.CreatedAtUtc,
                conversation.ListingId,
                ListingTitle = conversation.Listing.Title,
                ListingImageUrl = conversation.Listing.ImageUrl,
                ListingStatus = conversation.Listing.Status,
                OtherUserId = conversation.BuyerId == userId ? conversation.SellerId : conversation.BuyerId,
                OtherUserDisplayName = conversation.BuyerId == userId ? conversation.Seller.DisplayName : conversation.Buyer.DisplayName,
                LastMessage = conversation.Messages.OrderByDescending(message => message.CreatedAtUtc).Select(message => message.Body).FirstOrDefault(),
                conversation.LastMessageAtUtc,
                UnreadCount = conversation.Messages.Count(message => message.Type == ChatMessageType.User && message.SenderId != userId && message.ReadAtUtc == null)
            })
            .OrderByDescending(item => item.LastMessageAtUtc ?? item.InterestCreatedAtUtc)
            .ToListAsync(cancellationToken);

        return Ok(rows.Select(row => new InterestConversationResponse(
            row.InterestId, row.ConversationId, row.Direction, row.InterestCreatedAtUtc,
            row.ListingId, row.ListingTitle, row.ListingImageUrl, row.ListingStatus,
            row.OtherUserId, row.OtherUserDisplayName, row.LastMessage, row.LastMessageAtUtc, row.UnreadCount)));
    }
}
