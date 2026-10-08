using System.Security.Claims;
using Bricker.Api.Controllers;
using Bricker.Api.Contracts;
using Bricker.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Bricker.Api.Tests;

public sealed class InterestCreationIntegrationTests : IClassFixture<SqlServerTestDatabase>
{
    private readonly SqlServerTestDatabase database;

    public InterestCreationIntegrationTests(SqlServerTestDatabase database) => this.database = database;

    [Fact]
    public async Task Create_creates_one_interest_and_conversation_and_is_idempotent()
    {
        await using var setupContext = database.CreateContext();
        var seller = CreateUser("seller");
        var buyer = CreateUser("buyer");
        var category = new Category { Name = "Teste", Slug = $"teste-{Guid.NewGuid():N}" };
        var listing = new Listing
        {
            Category = category,
            Title = "Cimento excedente",
            Description = "Sacos em bom estado",
            Price = 42,
            Unit = "saco",
            Quantity = 10,
            Condition = MaterialCondition.Excellent,
            Status = ListingStatus.Active,
            City = "Brusque",
            State = "SC",
            SellerDisplayName = seller.DisplayName,
            SellerId = seller.Id
        };
        setupContext.Users.AddRange(seller, buyer);
        setupContext.Categories.Add(category);
        setupContext.Listings.Add(listing);
        await setupContext.SaveChangesAsync();

        var first = await CreateInterestAsync(listing.Id, buyer.Id);
        var second = await CreateInterestAsync(listing.Id, buyer.Id);

        Assert.Equal(first.InterestId, second.InterestId);
        Assert.Equal(first.ConversationId, second.ConversationId);

        await using var assertionContext = database.CreateContext();
        Assert.Equal(1, await assertionContext.ListingInterests.CountAsync(item => item.ListingId == listing.Id && item.InterestedUserId == buyer.Id));
        Assert.Equal(1, await assertionContext.Conversations.CountAsync(item => item.ListingInterestId == first.InterestId));
    }

    [Fact]
    public async Task Create_rejects_interest_in_own_listing()
    {
        await using var context = database.CreateContext();
        var seller = CreateUser("owner");
        var category = new Category { Name = "Teste", Slug = $"teste-{Guid.NewGuid():N}" };
        var listing = new Listing
        {
            Category = category,
            Title = "Material próprio",
            Description = "Descrição",
            Price = 20,
            Unit = "unidade",
            Quantity = 1,
            Status = ListingStatus.Active,
            City = "Brusque",
            State = "SC",
            SellerDisplayName = seller.DisplayName,
            SellerId = seller.Id
        };
        context.Users.Add(seller);
        context.Categories.Add(category);
        context.Listings.Add(listing);
        await context.SaveChangesAsync();

        var result = await CreateInterestResultAsync(listing.Id, seller.Id);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(0, await context.ListingInterests.CountAsync(item => item.ListingId == listing.Id));
    }

    private async Task<InterestCreatedResponse> CreateInterestAsync(Guid listingId, string userId)
    {
        var result = await CreateInterestResultAsync(listingId, userId);
        return Assert.IsType<OkObjectResult>(result.Result).Value as InterestCreatedResponse
            ?? throw new Xunit.Sdk.XunitException("A resposta de sucesso não contém os IDs criados.");
    }

    private async Task<ActionResult<InterestCreatedResponse>> CreateInterestResultAsync(Guid listingId, string userId)
    {
        await using var context = database.CreateContext();
        using var userStore = new TestUserStore(context);
        var userManager = new UserManager<AppUser>(
            userStore,
            Options.Create(new IdentityOptions()),
            new PasswordHasher<AppUser>(),
            [],
            [],
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            new ServiceCollection().BuildServiceProvider(),
            NullLogger<UserManager<AppUser>>.Instance);
        var controller = new InterestsController(context, userManager)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId)], "Test"))
                }
            }
        };
        return await controller.Create(listingId, CancellationToken.None);
    }

    private static AppUser CreateUser(string suffix) => new()
    {
        Id = Guid.NewGuid().ToString(),
        UserName = $"{suffix}-{Guid.NewGuid():N}@test.bricker",
        NormalizedUserName = $"{suffix}-{Guid.NewGuid():N}@TEST.BRICKER",
        Email = $"{suffix}-{Guid.NewGuid():N}@test.bricker",
        NormalizedEmail = $"{suffix}-{Guid.NewGuid():N}@TEST.BRICKER",
        DisplayName = suffix
    };
}
