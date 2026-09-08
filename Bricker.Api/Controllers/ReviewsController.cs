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
[Route("api/v1/sales")]
public sealed class SalesReviewsController(BrickerDbContext db, UserManager<AppUser> userManager) : ControllerBase
{
    [HttpGet("reviews/pending")]
    public async Task<ActionResult<IReadOnlyCollection<PendingReviewResponse>>> Pending(CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User)!;
        var pending = await db.ListingSales.AsNoTracking()
            .Where(sale => (sale.BuyerId == userId || sale.SellerId == userId) && !sale.Reviews.Any(review => review.ReviewerId == userId))
            .OrderByDescending(sale => sale.ConfirmedAtUtc)
            .Select(sale => new PendingReviewResponse(sale.Id, sale.ListingId, sale.Listing.Title,
                sale.BuyerId == userId ? sale.SellerId : sale.BuyerId,
                sale.BuyerId == userId ? sale.Seller.DisplayName : sale.Buyer.DisplayName,
                sale.BuyerId == userId ? "Vendedor" : "Comprador", sale.ConfirmedAtUtc))
            .ToListAsync(cancellationToken);
        return Ok(pending);
    }

    [HttpPost("{saleId:guid}/reviews")]
    public async Task<ActionResult<UserReviewResponse>> Create(Guid saleId, ReviewRequest request, CancellationToken cancellationToken)
    {
        var error = Validate(request);
        if (error is not null) return error;
        var userId = userManager.GetUserId(User)!;
        var sale = await db.ListingSales.Include(item => item.Listing).Include(item => item.Buyer).Include(item => item.Seller)
            .SingleOrDefaultAsync(item => item.Id == saleId && (item.BuyerId == userId || item.SellerId == userId), cancellationToken);
        if (sale is null) return NotFound();
        if (await db.UserReviews.AnyAsync(review => review.ListingSaleId == saleId && review.ReviewerId == userId, cancellationToken))
            return Conflict(new { message = "Você já avaliou esta negociação." });
        var revieweeId = sale.BuyerId == userId ? sale.SellerId : sale.BuyerId;
        var reviewer = sale.BuyerId == userId ? sale.Buyer : sale.Seller;
        var review = new UserReview
        {
            ListingSaleId = saleId,
            ReviewerId = userId,
            RevieweeId = revieweeId,
            Rating = request.Rating,
            Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim()
        };
        db.UserReviews.Add(review);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ToResponse(review, sale, reviewer.DisplayName, true));
    }

    private ActionResult? Validate(ReviewRequest request)
    {
        if (request.Rating is < 1 or > 5) ModelState.AddModelError(nameof(request.Rating), "A nota deve ser de 1 a 5.");
        if (request.Comment?.Trim().Length > 1_000) ModelState.AddModelError(nameof(request.Comment), "O comentário deve ter no máximo 1.000 caracteres.");
        return ModelState.IsValid ? null : ValidationProblem(ModelState);
    }

    internal static UserReviewResponse ToResponse(UserReview review, ListingSale sale, string reviewerName, bool canEdit) =>
        new(review.Id, sale.Id, review.ReviewerId, reviewerName, review.RevieweeId == sale.SellerId ? "Vendedor" : "Comprador",
            sale.Listing.Title, review.Rating, review.Comment, review.CreatedAtUtc, review.UpdatedAtUtc, canEdit);
}

[ApiController]
[Authorize]
[Route("api/v1/reviews")]
public sealed class ReviewsController(BrickerDbContext db, UserManager<AppUser> userManager) : ControllerBase
{
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UserReviewResponse>> Update(Guid id, ReviewRequest request, CancellationToken cancellationToken)
    {
        if (request.Rating is < 1 or > 5) return BadRequest(new { message = "A nota deve ser de 1 a 5." });
        if (request.Comment?.Trim().Length > 1_000) return BadRequest(new { message = "O comentário deve ter no máximo 1.000 caracteres." });
        var userId = userManager.GetUserId(User)!;
        var review = await db.UserReviews.Include(item => item.ListingSale).ThenInclude(sale => sale.Listing)
            .Include(item => item.Reviewer).SingleOrDefaultAsync(item => item.Id == id && item.ReviewerId == userId, cancellationToken);
        if (review is null) return NotFound();
        if (DateTime.UtcNow > review.CreatedAtUtc.AddDays(7)) return Conflict(new { message = "O prazo de 7 dias para editar esta avaliação terminou." });
        review.Rating = request.Rating;
        review.Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim();
        review.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(SalesReviewsController.ToResponse(review, review.ListingSale, review.Reviewer.DisplayName, true));
    }
}

[ApiController]
[Route("api/v1/users")]
public sealed class UsersController(BrickerDbContext db, UserManager<AppUser> userManager) : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<ActionResult<PublicUserProfileResponse>> Get(string id, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (user is null) return NotFound();
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);
        var query = db.UserReviews.AsNoTracking().Where(review => review.RevieweeId == id);
        var total = await query.CountAsync(cancellationToken);
        var rating = total == 0 ? null : await query.AverageAsync(review => (double?)review.Rating, cancellationToken);
        var currentUserId = userManager.GetUserId(User);
        var reviews = await query.OrderByDescending(review => review.CreatedAtUtc).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(review => new UserReviewResponse(review.Id, review.ListingSaleId, review.ReviewerId, review.Reviewer.DisplayName,
                review.RevieweeId == review.ListingSale.SellerId ? "Vendedor" : "Comprador", review.ListingSale.Listing.Title,
                review.Rating, review.Comment, review.CreatedAtUtc, review.UpdatedAtUtc,
                review.ReviewerId == currentUserId && DateTime.UtcNow <= review.CreatedAtUtc.AddDays(7)))
            .ToListAsync(cancellationToken);
        return Ok(new PublicUserProfileResponse(user.Id, user.DisplayName, user.City, user.State, user.CreatedAtUtc, rating, total, reviews, page, pageSize, total));
    }
}
