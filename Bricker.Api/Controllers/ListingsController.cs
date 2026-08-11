using Bricker.Api.Contracts;
using Bricker.Api.Data;
using Bricker.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bricker.Api.Controllers;

[ApiController]
[Route("api/v1/listings")]
public sealed class ListingsController(BrickerDbContext db, UserManager<AppUser> userManager, IWebHostEnvironment environment) : ControllerBase
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

    [Authorize]
    [HttpGet("mine")]
    public async Task<ActionResult<IReadOnlyCollection<ListingResponse>>> Mine(CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User);
        var listings = await db.Listings.AsNoTracking().Include(listing => listing.Category)
            .Where(listing => listing.SellerId == userId)
            .OrderByDescending(listing => listing.CreatedAtUtc)
            .Select(listing => ToResponse(listing))
            .ToListAsync(cancellationToken);

        return Ok(listings);
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<ListingResponse>> Create([FromForm] UpsertListingRequest request, CancellationToken cancellationToken)
    {
        var validation = Validate(request);
        if (validation is not null) return validation;

        var category = await db.Categories.SingleOrDefaultAsync(item => item.Id == request.CategoryId && item.IsActive, cancellationToken);
        if (category is null)
        {
            ModelState.AddModelError(nameof(request.CategoryId), "Categoria inválida.");
            return ValidationProblem(ModelState);
        }

        var user = await userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        var listing = new Listing
        {
            CategoryId = category.Id,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Price = request.Price,
            Unit = request.Unit.Trim(),
            Quantity = request.Quantity,
            Condition = request.Condition,
            Status = ListingStatus.Active,
            City = request.City.Trim(),
            State = request.State.Trim().ToUpperInvariant(),
            SellerId = user.Id,
            SellerDisplayName = user.DisplayName,
            ImageUrl = await SaveImage(request.Image, cancellationToken)
        };

        db.Listings.Add(listing);
        await db.SaveChangesAsync(cancellationToken);
        listing.Category = category;

        return CreatedAtAction(nameof(GetById), new { id = listing.Id }, ToResponse(listing));
    }

    [Authorize]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ListingResponse>> Update(Guid id, [FromForm] UpsertListingRequest request, CancellationToken cancellationToken)
    {
        var validation = Validate(request);
        if (validation is not null) return validation;

        var userId = userManager.GetUserId(User);
        var listing = await db.Listings.Include(item => item.Category)
            .SingleOrDefaultAsync(item => item.Id == id && item.SellerId == userId, cancellationToken);
        if (listing is null) return NotFound();

        var category = await db.Categories.SingleOrDefaultAsync(item => item.Id == request.CategoryId && item.IsActive, cancellationToken);
        if (category is null)
        {
            ModelState.AddModelError(nameof(request.CategoryId), "Categoria inválida.");
            return ValidationProblem(ModelState);
        }

        listing.CategoryId = category.Id;
        listing.Category = category;
        listing.Title = request.Title.Trim();
        listing.Description = request.Description.Trim();
        listing.Price = request.Price;
        listing.Unit = request.Unit.Trim();
        listing.Quantity = request.Quantity;
        listing.Condition = request.Condition;
        listing.City = request.City.Trim();
        listing.State = request.State.Trim().ToUpperInvariant();
        var imageUrl = await SaveImage(request.Image, cancellationToken);
        if (imageUrl is not null)
        {
            DeleteImage(listing.ImageUrl);
            listing.ImageUrl = imageUrl;
        }
        listing.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return Ok(ToResponse(listing));
    }

    [Authorize]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User);
        var listing = await db.Listings.SingleOrDefaultAsync(item => item.Id == id && item.SellerId == userId, cancellationToken);
        if (listing is null) return NotFound();

        listing.Status = ListingStatus.Inactive;
        listing.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private ActionResult? Validate(UpsertListingRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title)) ModelState.AddModelError(nameof(request.Title), "Informe um título.");
        if (string.IsNullOrWhiteSpace(request.Description)) ModelState.AddModelError(nameof(request.Description), "Informe uma descrição.");
        if (string.IsNullOrWhiteSpace(request.Unit)) ModelState.AddModelError(nameof(request.Unit), "Informe uma unidade.");
        if (string.IsNullOrWhiteSpace(request.City)) ModelState.AddModelError(nameof(request.City), "Informe uma cidade.");
        if (string.IsNullOrWhiteSpace(request.State) || request.State.Trim().Length != 2) ModelState.AddModelError(nameof(request.State), "Informe a UF com duas letras.");
        if (request.Price <= 0) ModelState.AddModelError(nameof(request.Price), "O preço deve ser maior que zero.");
        if (request.Quantity <= 0) ModelState.AddModelError(nameof(request.Quantity), "A quantidade deve ser maior que zero.");
        return ModelState.IsValid ? null : ValidationProblem(ModelState);
    }

    private static ListingResponse ToResponse(Listing listing) => new(
        listing.Id, listing.Title, listing.Description, listing.Price, listing.Unit, listing.Quantity,
        listing.Condition, listing.Status, listing.City, listing.State, listing.Category.Name,
        listing.Category.Slug, listing.SellerDisplayName, listing.ImageUrl, listing.CreatedAtUtc);

    private async Task<string?> SaveImage(IFormFile? image, CancellationToken cancellationToken)
    {
        if (image is null || image.Length == 0) return null;
        if (image.Length > 5 * 1024 * 1024) throw new BadHttpRequestException("A imagem deve ter no máximo 5 MB.");

        var extensions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = ".jpg", ["image/png"] = ".png", ["image/webp"] = ".webp"
        };
        if (!extensions.TryGetValue(image.ContentType, out var extension))
            throw new BadHttpRequestException("Envie uma imagem JPG, PNG ou WEBP.");

        var relativeFolder = Path.Combine("uploads", "listings");
        var folder = Path.Combine(environment.ContentRootPath, relativeFolder);
        Directory.CreateDirectory(folder);
        var fileName = $"{Guid.NewGuid():N}{extension}";
        await using var stream = System.IO.File.Create(Path.Combine(folder, fileName));
        await image.CopyToAsync(stream, cancellationToken);
        return $"/{relativeFolder.Replace('\\', '/')}/{fileName}";
    }

    private void DeleteImage(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl)) return;
        var relativePath = imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(environment.ContentRootPath, relativePath);
        if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);
    }
}
