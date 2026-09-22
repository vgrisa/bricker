using Bricker.Api.Contracts;
using Bricker.Api.Data;
using Bricker.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Bricker.Api.Validation;
using Bricker.Api.Hubs;
using Microsoft.AspNetCore.SignalR;
using Bricker.Api.Storage;

namespace Bricker.Api.Controllers;

[ApiController]
[Route("api/v1/listings")]
public sealed class ListingsController(BrickerDbContext db, UserManager<AppUser> userManager, UploadStorage uploadStorage, IHubContext<ChatHub> hub) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<ListingResponse>>> Search(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] string? location,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] string? condition,
        [FromQuery] string? sort,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);
        if (search?.Length > 160 || location?.Length > 160 || category?.Length > 300 || condition?.Length > 30)
            return BadRequest(new { message = "Um ou mais filtros excedem o tamanho permitido." });

        var query = db.Listings.AsNoTracking().Include(listing => listing.Category).Include(listing => listing.Images)
            .Where(listing => listing.Status == ListingStatus.Active);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(listing =>
                listing.Title.Contains(term) ||
                listing.Description.Contains(term) ||
                listing.SellerDisplayName.Contains(term));
        }

        var categorySlugs = SplitValues(category).Select(value => value.ToLowerInvariant()).ToArray();
        if (categorySlugs.Length > 0)
            query = query.Where(listing => categorySlugs.Contains(listing.Category.Slug));

        if (!string.IsNullOrWhiteSpace(location))
        {
            var term = location.Trim();
            var postalCode = InputValidation.Digits(term);
            query = query.Where(listing =>
                listing.City.Contains(term) ||
                listing.State.Contains(term) ||
                (listing.Neighborhood != null && listing.Neighborhood.Contains(term)) ||
                (listing.Street != null && listing.Street.Contains(term)) ||
                (postalCode.Length > 0 && listing.PostalCode != null && listing.PostalCode.Contains(postalCode)));
        }
        if (minPrice is not null) query = query.Where(listing => listing.Price >= minPrice);
        if (maxPrice is not null) query = query.Where(listing => listing.Price <= maxPrice);
        var conditions = SplitValues(condition)
            .Select(value => int.TryParse(value, out var numeric) && Enum.IsDefined(typeof(MaterialCondition), numeric)
                ? (MaterialCondition?)numeric
                : null)
            .Where(value => value is not null)
            .Select(value => value!.Value)
            .ToArray();
        if (!string.IsNullOrWhiteSpace(condition) && conditions.Length == 0)
            return BadRequest(new { message = "A condição informada é inválida." });
        if (conditions.Length > 0) query = query.Where(listing => conditions.Contains(listing.Condition));

        var totalCount = await query.CountAsync(cancellationToken);
        query = sort switch
        {
            "priceAsc" => query.OrderBy(listing => listing.Price),
            "priceDesc" => query.OrderByDescending(listing => listing.Price),
            _ => query.OrderByDescending(listing => listing.CreatedAtUtc)
        };
        var entities = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        var items = entities.Select(ToResponse).ToList();

        return Ok(new PagedResponse<ListingResponse>(items, page, pageSize, totalCount));
    }

    private static IEnumerable<string> SplitValues(string? values) =>
        (values ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ListingResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var entity = await db.Listings.AsNoTracking().Include(item => item.Category).Include(item => item.Images)
            .SingleOrDefaultAsync(item => item.Id == id && item.Status == ListingStatus.Active, cancellationToken);
        var listing = entity is null ? null : ToResponse(entity);

        return listing is null ? NotFound() : Ok(listing);
    }

    [HttpGet("{id:guid}/details")]
    public async Task<ActionResult<ListingDetailResponse>> Details(Guid id, CancellationToken cancellationToken)
    {
        var listing = await db.Listings.AsNoTracking().Include(item => item.Category).Include(item => item.Images).Include(item => item.Seller)
            .SingleOrDefaultAsync(item => item.Id == id && item.Status == ListingStatus.Active, cancellationToken);
        if (listing is null) return NotFound();
        var images = listing.Images.OrderBy(image => image.SortOrder).Select(image => new ListingImageResponse(image.Id, image.Url, image.SortOrder)).ToList();
        if (images.Count == 0 && listing.ImageUrl is not null) images.Add(new ListingImageResponse(Guid.Empty, listing.ImageUrl, 0));
        SellerResponse? seller = null;
        if (listing.Seller is not null)
        {
            var reviewQuery = db.UserReviews.AsNoTracking().Where(review => review.RevieweeId == listing.Seller.Id);
            var reviewCount = await reviewQuery.CountAsync(cancellationToken);
            var rating = reviewCount == 0 ? null : await reviewQuery.AverageAsync(review => (double?)review.Rating, cancellationToken);
            seller = new SellerResponse(listing.Seller.Id, listing.Seller.DisplayName, listing.Seller.City, listing.Seller.State, rating, reviewCount, listing.Seller.CreatedAtUtc);
        }
        return Ok(new ListingDetailResponse(ToResponse(listing), images, seller));
    }

    [Authorize]
    [HttpGet("mine")]
    public async Task<ActionResult<IReadOnlyCollection<ListingResponse>>> Mine(CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User);
        var entities = await db.Listings.AsNoTracking().Include(listing => listing.Category).Include(listing => listing.Images).Include(listing => listing.Sale)
            .Where(listing => listing.SellerId == userId)
            .OrderByDescending(listing => listing.CreatedAtUtc)
            .ToListAsync(cancellationToken);
        var listings = entities.Select(ToResponse).ToList();

        return Ok(listings);
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<ListingResponse>> Create([FromForm] UpsertListingRequest request, CancellationToken cancellationToken)
    {
        var validation = Validate(request);
        if (validation is not null) return validation;
        if (request.Images is null || request.Images.Count == 0)
            return BadRequest(new { message = "Adicione pelo menos uma foto ao anúncio." });

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
            PostalCode = InputValidation.Digits(request.PostalCode),
            Street = request.Street!.Trim(),
            Neighborhood = request.Neighborhood!.Trim(),
            AddressNumber = request.AddressNumber!.Trim(),
            AddressComplement = string.IsNullOrWhiteSpace(request.AddressComplement) ? null : request.AddressComplement.Trim(),
            SellerId = user.Id,
            SellerDisplayName = user.DisplayName,
            ImageUrl = null
        };
        await AddImages(listing, request.Images, cancellationToken);
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
        var listing = await db.Listings.Include(item => item.Category).Include(item => item.Images)
            .SingleOrDefaultAsync(item => item.Id == id && item.SellerId == userId && item.Status == ListingStatus.Active, cancellationToken);
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
        listing.PostalCode = InputValidation.Digits(request.PostalCode);
        listing.Street = request.Street!.Trim();
        listing.Neighborhood = request.Neighborhood!.Trim();
        listing.AddressNumber = request.AddressNumber!.Trim();
        listing.AddressComplement = string.IsNullOrWhiteSpace(request.AddressComplement) ? null : request.AddressComplement.Trim();
        if (request.NewCoverIndex is int newCoverIndex)
        {
            if (newCoverIndex < 0 || newCoverIndex >= (request.Images?.Count ?? 0))
                return BadRequest(new { message = "A foto de capa selecionada é inválida." });
        }
        var addedImages = await AddImages(listing, request.Images, cancellationToken);
        if (request.NewCoverIndex is int selectedCoverIndex)
        {
            var cover = addedImages[selectedCoverIndex];
            var orderedImages = listing.Images.OrderBy(image => image.SortOrder).ToList();
            orderedImages.Remove(cover);
            orderedImages.Insert(0, cover);
            for (var index = 0; index < orderedImages.Count; index++) orderedImages[index].SortOrder = index;
            listing.ImageUrl = cover.Url;
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

        await SetListingStatus(listing, ListingStatus.Inactive, cancellationToken);
        return NoContent();
    }

    [Authorize]
    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, UpdateListingStatusRequest request, CancellationToken cancellationToken)
    {
        if (request.Status is ListingStatus.Sold)
            return BadRequest(new { message = "Selecione o comprador para concluir a venda." });
        if (request.Status is not (ListingStatus.Active or ListingStatus.Reserved or ListingStatus.Inactive))
            return BadRequest(new { message = "Status inválido." });
        var userId = userManager.GetUserId(User);
        var listing = await db.Listings.Include(item => item.Sale).SingleOrDefaultAsync(item => item.Id == id && item.SellerId == userId, cancellationToken);
        if (listing is null) return NotFound();
        if (listing.Sale is not null) return Conflict(new { message = "Uma venda confirmada não pode ter o status alterado." });
        await SetListingStatus(listing, request.Status, cancellationToken);
        return NoContent();
    }

    [Authorize]
    [HttpPost("{id:guid}/complete-sale")]
    public async Task<ActionResult<SaleResponse>> CompleteSale(Guid id, CompleteSaleRequest request, CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User)!;
        var listing = await db.Listings.Include(item => item.Sale)
            .SingleOrDefaultAsync(item => item.Id == id && item.SellerId == userId, cancellationToken);
        if (listing is null) return NotFound();
        if (listing.Sale is not null) return Conflict(new { message = "A venda deste anúncio já foi confirmada." });
        if (listing.Status is ListingStatus.Draft or ListingStatus.Inactive)
            return Conflict(new { message = "Ative o anúncio antes de concluir a venda." });
        var interest = await db.ListingInterests.Include(item => item.InterestedUser)
            .SingleOrDefaultAsync(item => item.Id == request.InterestId && item.ListingId == id, cancellationToken);
        if (interest is null) return BadRequest(new { message = "Selecione um interessado válido deste anúncio." });
        var seller = await userManager.GetUserAsync(User);
        if (seller is null) return Unauthorized();
        var sale = new ListingSale
        {
            ListingId = id,
            ListingInterestId = interest.Id,
            BuyerId = interest.InterestedUserId,
            SellerId = userId
        };
        db.ListingSales.Add(sale);
        await SetListingStatus(listing, ListingStatus.Sold, cancellationToken);
        return Ok(new SaleResponse(sale.Id, id, listing.Title, sale.BuyerId, interest.InterestedUser.DisplayName,
            sale.SellerId, seller.DisplayName, sale.ConfirmedAtUtc));
    }

    [Authorize]
    [HttpDelete("{id:guid}/images/{imageId:guid}")]
    public async Task<IActionResult> RemoveImage(Guid id, Guid imageId, CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User);
        var image = await db.ListingImages.Include(item => item.Listing).SingleOrDefaultAsync(item => item.Id == imageId && item.ListingId == id && item.Listing.SellerId == userId, cancellationToken);
        if (image is null) return NotFound();
        if (await db.ListingImages.CountAsync(item => item.ListingId == id, cancellationToken) <= 1)
            return BadRequest(new { message = "O anúncio precisa manter pelo menos uma foto." });
        uploadStorage.Delete(image.Url);
        db.ListingImages.Remove(image);
        if (image.Listing.ImageUrl == image.Url) image.Listing.ImageUrl = await db.ListingImages.Where(item => item.ListingId == id && item.Id != imageId).OrderBy(item => item.SortOrder).Select(item => item.Url).FirstOrDefaultAsync(cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [Authorize]
    [HttpPut("{id:guid}/images/order")]
    public async Task<IActionResult> ReorderImages(Guid id, ReorderImagesRequest request, CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User);
        var listing = await db.Listings.Include(item => item.Images).SingleOrDefaultAsync(item => item.Id == id && item.SellerId == userId, cancellationToken);
        if (listing is null) return NotFound();
        if (request.ImageIds.Count != listing.Images.Count || request.ImageIds.Distinct().Count() != request.ImageIds.Count || request.ImageIds.Except(listing.Images.Select(image => image.Id)).Any()) return BadRequest(new { message = "A ordem das imagens é inválida." });
        foreach (var image in listing.Images) image.SortOrder = request.ImageIds.ToList().IndexOf(image.Id);
        listing.ImageUrl = listing.Images.OrderBy(image => image.SortOrder).FirstOrDefault()?.Url;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private ActionResult? Validate(UpsertListingRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title)) ModelState.AddModelError(nameof(request.Title), "Informe um título.");
        else if (request.Title.Trim().Length is < 5 or > 160) ModelState.AddModelError(nameof(request.Title), "O título deve ter entre 5 e 160 caracteres.");
        if (string.IsNullOrWhiteSpace(request.Description)) ModelState.AddModelError(nameof(request.Description), "Informe uma descrição.");
        else if (request.Description.Trim().Length is < 20 or > 2_000) ModelState.AddModelError(nameof(request.Description), "A descrição deve ter entre 20 e 2.000 caracteres.");
        if (string.IsNullOrWhiteSpace(request.Unit)) ModelState.AddModelError(nameof(request.Unit), "Informe uma unidade.");
        else if (request.Unit.Trim().Length > 24) ModelState.AddModelError(nameof(request.Unit), "A unidade deve ter no máximo 24 caracteres.");
        if (string.IsNullOrWhiteSpace(request.City)) ModelState.AddModelError(nameof(request.City), "Informe uma cidade.");
        else if (request.City.Trim().Length is < 2 or > 100) ModelState.AddModelError(nameof(request.City), "A cidade deve ter entre 2 e 100 caracteres.");
        if (!InputValidation.IsValidState(request.State)) ModelState.AddModelError(nameof(request.State), "Informe a UF com duas letras.");
        if (!InputValidation.IsValidPostalCode(request.PostalCode)) ModelState.AddModelError(nameof(request.PostalCode), "Informe um CEP válido com 8 números.");
        if (string.IsNullOrWhiteSpace(request.Street) || request.Street.Trim().Length is < 2 or > 150) ModelState.AddModelError(nameof(request.Street), "Informe um logradouro válido.");
        if (string.IsNullOrWhiteSpace(request.Neighborhood) || request.Neighborhood.Trim().Length is < 2 or > 100) ModelState.AddModelError(nameof(request.Neighborhood), "Informe um bairro válido.");
        if (string.IsNullOrWhiteSpace(request.AddressNumber) || request.AddressNumber.Trim().Length > 20) ModelState.AddModelError(nameof(request.AddressNumber), "Informe o número do endereço.");
        if (request.AddressComplement?.Trim().Length > 100) ModelState.AddModelError(nameof(request.AddressComplement), "O complemento deve ter no máximo 100 caracteres.");
        if (request.Price <= 0) ModelState.AddModelError(nameof(request.Price), "O preço deve ser maior que zero.");
        else if (request.Price > 9_999_999_999.99m) ModelState.AddModelError(nameof(request.Price), "O preço informado é muito alto.");
        if (request.Quantity <= 0) ModelState.AddModelError(nameof(request.Quantity), "A quantidade deve ser maior que zero.");
        else if (request.Quantity > 9_999_999_999.99m) ModelState.AddModelError(nameof(request.Quantity), "A quantidade informada é muito alta.");
        if (!Enum.IsDefined(request.Condition)) ModelState.AddModelError(nameof(request.Condition), "Condição inválida.");
        return ModelState.IsValid ? null : ValidationProblem(ModelState);
    }

    private static ListingResponse ToResponse(Listing listing) => new(
        listing.Id, listing.Title, listing.Description, listing.Price, listing.Unit, listing.Quantity,
        listing.Condition, listing.Status, listing.City, listing.State, listing.PostalCode,
        listing.Street, listing.Neighborhood, listing.AddressNumber, listing.AddressComplement, listing.Category.Name,
        listing.Category.Slug, listing.SellerDisplayName, listing.ImageUrl,
        listing.Images.OrderBy(image => image.SortOrder).Select(image => image.Url).ToList(), listing.CreatedAtUtc)
        { HasConfirmedSale = listing.Sale is not null };

    private async Task<IReadOnlyList<ListingImage>> AddImages(Listing listing, IReadOnlyCollection<IFormFile>? images, CancellationToken cancellationToken)
    {
        if (images is null || images.Count == 0) return [];
        var addedImages = new List<ListingImage>();
        var existingCount = await db.ListingImages.CountAsync(item => item.ListingId == listing.Id, cancellationToken);
        if (existingCount + images.Count > 5) throw new BadHttpRequestException("Um anúncio pode ter no máximo 5 imagens.");
        foreach (var image in images.Where(image => image.Length > 0))
        {
            if (image.Length > 5 * 1024 * 1024) throw new BadHttpRequestException("Cada imagem deve ter no máximo 5 MB.");

        var extensions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = ".jpg", ["image/png"] = ".png", ["image/webp"] = ".webp"
        };
            if (!extensions.TryGetValue(image.ContentType, out var extension)) throw new BadHttpRequestException("Envie imagens JPG, PNG ou WEBP.");

            var url = await uploadStorage.SaveListingImageAsync(image, extension, cancellationToken);
            var listingImage = new ListingImage { Url = url, SortOrder = existingCount++ };
            listing.Images.Add(listingImage);
            addedImages.Add(listingImage);
            listing.ImageUrl ??= url;
        }
        return addedImages;
    }

    private async Task SetListingStatus(Listing listing, ListingStatus status, CancellationToken cancellationToken)
    {
        if (listing.Status == status)
        {
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        listing.Status = status;
        listing.UpdatedAtUtc = DateTime.UtcNow;
        var body = status switch
        {
            ListingStatus.Active => "Este material voltou a ficar disponível.",
            ListingStatus.Reserved => "Este material foi reservado.",
            ListingStatus.Sold => "Este material foi vendido.",
            ListingStatus.Inactive => "Este material foi inativado.",
            _ => null
        };
        var notifications = new List<(Conversation Conversation, ChatMessage Message)>();
        if (body is not null)
        {
            var conversations = await db.Conversations
                .Where(conversation => conversation.ListingId == listing.Id)
                .ToListAsync(cancellationToken);
            foreach (var conversation in conversations)
            {
                var message = new ChatMessage
                {
                    ConversationId = conversation.Id,
                    Body = body,
                    Type = ChatMessageType.System
                };
                conversation.LastMessageAtUtc = message.CreatedAtUtc;
                db.ChatMessages.Add(message);
                notifications.Add((conversation, message));
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        foreach (var (conversation, message) in notifications)
        {
            var response = new ChatMessageResponse(message.Id, conversation.Id, null, null, message.Body,
                message.CreatedAtUtc, null, message.Type);
            await hub.Clients.Group(ChatHub.GroupName(conversation.Id))
                .SendAsync("MessageReceived", response, cancellationToken);
            await hub.Clients.Groups(ChatHub.UserGroup(conversation.BuyerId), ChatHub.UserGroup(conversation.SellerId))
                .SendAsync("ConversationUpdated", conversation.Id, cancellationToken);
        }
    }
}
