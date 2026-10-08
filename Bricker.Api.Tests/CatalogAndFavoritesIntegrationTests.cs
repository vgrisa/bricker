using Bricker.Api.Controllers;
using Bricker.Api.Contracts;
using Bricker.Api.Models;
using Bricker.Api.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bricker.Api.Tests;

public sealed class CatalogAndFavoritesIntegrationTests : IClassFixture<SqlServerTestDatabase>
{
    private readonly SqlServerTestDatabase database;

    public CatalogAndFavoritesIntegrationTests(SqlServerTestDatabase database) => this.database = database;

    [Fact]
    public async Task Search_filters_by_multiple_category_location_and_price()
    {
        await using var context = database.CreateContext();
        var seller = TestSupport.User("vendedor");
        var electrical = TestSupport.Category();
        var wood = new Category { Name = "Madeira", Slug = $"madeira-{Guid.NewGuid():N}" };
        var matching = TestSupport.Listing(seller, electrical);
        var other = TestSupport.Listing(seller, wood);
        other.City = "Itajaí";
        other.Price = 900m;
        context.AddRange(seller, electrical, wood, matching, other);
        await context.SaveChangesAsync();

        var result = await Listings(context).Search("cabos", electrical.Slug, "brusque", 100, 200, "0", "priceAsc", 1, 12);

        var response = Assert.IsType<OkObjectResult>(result.Result).Value as PagedResponse<ListingResponse>;
        Assert.NotNull(response);
        Assert.Single(response.Items);
        Assert.Equal(matching.Id, response.Items.Single().Id);
    }

    [Fact]
    public async Task Favorite_is_idempotent_and_can_be_removed()
    {
        await using var context = database.CreateContext();
        var seller = TestSupport.User("vendedor");
        var buyer = TestSupport.User("comprador");
        var category = TestSupport.Category();
        var listing = TestSupport.Listing(seller, category);
        context.AddRange(seller, buyer, category, listing);
        await context.SaveChangesAsync();
        using var store = new TestUserStore(context);
        var controller = new FavoritesController(context, TestSupport.UserManager(store))
        {
            ControllerContext = new() { HttpContext = TestSupport.UserContext(buyer.Id) }
        };

        Assert.IsType<NoContentResult>(await controller.Add(listing.Id, CancellationToken.None));
        Assert.IsType<NoContentResult>(await controller.Add(listing.Id, CancellationToken.None));
        Assert.Equal(1, await context.ListingFavorites.CountAsync());
        Assert.IsType<NoContentResult>(await controller.Remove(listing.Id, CancellationToken.None));
        Assert.Empty(await context.ListingFavorites.ToListAsync());
    }

    [Fact]
    public async Task Update_does_not_allow_a_different_seller()
    {
        await using var context = database.CreateContext();
        var seller = TestSupport.User("vendedor");
        var otherUser = TestSupport.User("terceiro");
        var category = TestSupport.Category();
        var listing = TestSupport.Listing(seller, category);
        context.AddRange(seller, otherUser, category, listing);
        await context.SaveChangesAsync();
        using var store = new TestUserStore(context);
        var controller = Listings(context);
        controller.ControllerContext = new() { HttpContext = TestSupport.UserContext(otherUser.Id) };
        var request = new UpsertListingRequest(category.Id, listing.Title, listing.Description, listing.Price, listing.Unit,
            listing.Quantity, listing.Condition, listing.City, listing.State, listing.PostalCode, listing.Street,
            listing.Neighborhood, listing.AddressNumber, null, null, null);

        var result = await controller.Update(listing.Id, request, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    private static ListingsController Listings(Bricker.Api.Data.BrickerDbContext context) => new(
        context, TestSupport.UserManager(new TestUserStore(context)),
        new UploadStorage(Path.Combine(Path.GetTempPath(), "bricker-tests", Guid.NewGuid().ToString("N"))), TestSupport.Hub());
}
