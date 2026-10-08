using Bricker.Api.Contracts;
using Bricker.Api.Data;
using Bricker.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using Bricker.Api.Services;

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
        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            // A retry can follow a failed commit after SaveChanges accepted the tracked entities.
            // Start each execution attempt from the database state instead of reusing those entities.
            db.ChangeTracker.Clear();
            var listing = await db.Listings.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
            switch (InterestCreationPolicy.Evaluate(listing, user.Id))
            {
                case InterestCreationDecision.ListingUnavailable:
                    return (ActionResult<InterestCreatedResponse>)NotFound();
                case InterestCreationDecision.OwnListing:
                    return (ActionResult<InterestCreatedResponse>)BadRequest(new { message = "Você não pode demonstrar interesse no próprio anúncio." });
                case InterestCreationDecision.SellerUnavailable:
                    return (ActionResult<InterestCreatedResponse>)BadRequest(new { message = "Este anúncio não possui um vendedor disponível para conversa." });
            }

            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var interest = await db.ListingInterests.Include(item => item.Conversation)
                .SingleOrDefaultAsync(item => item.ListingId == id && item.InterestedUserId == user.Id, cancellationToken);
            if (interest?.Conversation is not null)
            {
                await transaction.CommitAsync(cancellationToken);
                return (ActionResult<InterestCreatedResponse>)Ok(new InterestCreatedResponse(interest.Id, interest.Conversation.Id));
            }

            interest ??= new ListingInterest { ListingId = id, InterestedUserId = user.Id };
            if (db.Entry(interest).State == EntityState.Detached) db.ListingInterests.Add(interest);
            var conversation = new Conversation
            {
                ListingInterestId = interest.Id,
                ListingId = listing!.Id,
                BuyerId = user.Id,
                SellerId = listing.SellerId!
            };
            db.Conversations.Add(conversation);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return (ActionResult<InterestCreatedResponse>)Ok(new InterestCreatedResponse(interest.Id, conversation.Id));
        });
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
        var now = DateTime.UtcNow;
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

        var interestIds = rows.Select(row => row.InterestId).ToList();
        var sales = await db.ListingSales.AsNoTracking()
            .Include(sale => sale.Listing)
            .Include(sale => sale.Reviews)
                .ThenInclude(review => review.Reviewer)
            .Where(sale => interestIds.Contains(sale.ListingInterestId))
            .ToDictionaryAsync(sale => sale.ListingInterestId, cancellationToken);

        return Ok(rows.Select(row =>
        {
            InterestReviewContextResponse? reviewContext = null;
            if (sales.TryGetValue(row.InterestId, out var sale))
            {
                var myReview = sale.Reviews.SingleOrDefault(review => review.ReviewerId == userId);
                var review = myReview is null
                    ? null
                    : new UserReviewResponse(myReview.Id, sale.Id, myReview.ReviewerId, myReview.Reviewer.DisplayName,
                        myReview.RevieweeId == sale.SellerId ? "Vendedor" : "Comprador", sale.Listing.Title,
                        myReview.Rating, myReview.Comment, myReview.CreatedAtUtc, myReview.UpdatedAtUtc,
                        now <= myReview.CreatedAtUtc.AddDays(7));
                reviewContext = new InterestReviewContextResponse(sale.Id, review is null ? "pending" : "completed",
                    row.OtherUserId, row.OtherUserDisplayName, sale.BuyerId == userId ? "Vendedor" : "Comprador",
                    sale.ConfirmedAtUtc, review);
            }

            return new InterestConversationResponse(row.InterestId, row.ConversationId, row.Direction,
                row.InterestCreatedAtUtc, row.ListingId, row.ListingTitle, row.ListingImageUrl, row.ListingStatus,
                row.OtherUserId, row.OtherUserDisplayName, row.LastMessage, row.LastMessageAtUtc, row.UnreadCount,
                reviewContext);
        }));
    }
}
