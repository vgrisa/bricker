using Bricker.Api.Controllers;
using Bricker.Api.Contracts;
using Bricker.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bricker.Api.Tests;

public sealed class MessagingCommerceIntegrationTests : IClassFixture<SqlServerTestDatabase>
{
    private readonly SqlServerTestDatabase database;

    public MessagingCommerceIntegrationTests(SqlServerTestDatabase database) => this.database = database;

    [Fact]
    public async Task Conversation_allows_participant_to_send_and_mark_messages_as_read()
    {
        await using var context = database.CreateContext();
        var (seller, buyer, _, _, conversation) = await CreateConversation(context);
        using var store = new TestUserStore(context);
        var controller = new ConversationsController(context, TestSupport.UserManager(store), TestSupport.Hub())
        {
            ControllerContext = new() { HttpContext = TestSupport.UserContext(buyer.Id) }
        };

        var sent = await controller.Send(conversation.Id, new SendMessageRequest("  Olá, ainda está disponível?  "), CancellationToken.None);
        Assert.Equal("Olá, ainda está disponível?", Assert.IsType<OkObjectResult>(sent.Result).Value is ChatMessageResponse message ? message.Body : null);

        controller.ControllerContext = new() { HttpContext = TestSupport.UserContext(seller.Id) };
        Assert.IsType<NoContentResult>(await controller.MarkRead(conversation.Id, CancellationToken.None));
        Assert.NotNull(await context.ChatMessages.AsNoTracking().Where(item => item.ConversationId == conversation.Id)
            .Select(item => item.ReadAtUtc).SingleAsync());
    }

    [Fact]
    public async Task Conversation_rejects_non_participant_and_messages_over_limit()
    {
        await using var context = database.CreateContext();
        var (_, _, _, _, conversation) = await CreateConversation(context);
        var outsider = TestSupport.User("terceiro");
        context.Users.Add(outsider);
        await context.SaveChangesAsync();
        using var store = new TestUserStore(context);
        var controller = new ConversationsController(context, TestSupport.UserManager(store), TestSupport.Hub())
        {
            ControllerContext = new() { HttpContext = TestSupport.UserContext(outsider.Id) }
        };

        Assert.IsType<NotFoundResult>((await controller.Send(conversation.Id, new SendMessageRequest("oi"), CancellationToken.None)).Result);
        controller.ControllerContext = new() { HttpContext = TestSupport.UserContext(conversation.BuyerId) };
        Assert.IsType<BadRequestObjectResult>((await controller.Send(conversation.Id, new SendMessageRequest(new string('a', 2_001)), CancellationToken.None)).Result);
    }

    [Fact]
    public async Task Complete_sale_once_and_create_one_review_per_participant()
    {
        await using var context = database.CreateContext();
        var (seller, buyer, _, listing, conversation) = await CreateConversation(context);
        var interest = await context.ListingInterests.SingleAsync(item => item.ListingId == listing.Id);
        using var store = new TestUserStore(context);
        var listings = new ListingsController(context, TestSupport.UserManager(store), new Bricker.Api.Storage.UploadStorage(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))), TestSupport.Hub())
        {
            ControllerContext = new() { HttpContext = TestSupport.UserContext(seller.Id) }
        };

        var saleResult = await listings.CompleteSale(listing.Id, new CompleteSaleRequest(interest.Id), CancellationToken.None);
        var sale = Assert.IsType<OkObjectResult>(saleResult.Result).Value as SaleResponse;
        Assert.NotNull(sale);
        Assert.Equal(ListingStatus.Sold, (await context.Listings.SingleAsync(item => item.Id == listing.Id)).Status);
        Assert.IsType<ConflictObjectResult>((await listings.CompleteSale(listing.Id, new CompleteSaleRequest(interest.Id), CancellationToken.None)).Result);

        var reviews = new SalesReviewsController(context, TestSupport.UserManager(store))
        {
            ControllerContext = new() { HttpContext = TestSupport.UserContext(buyer.Id) }
        };
        Assert.IsType<OkObjectResult>((await reviews.Create(sale.Id, new ReviewRequest(5, "Negociação ótima"), CancellationToken.None)).Result);
        Assert.IsType<ConflictObjectResult>((await reviews.Create(sale.Id, new ReviewRequest(5, null), CancellationToken.None)).Result);
        Assert.IsType<ObjectResult>((await reviews.Create(sale.Id, new ReviewRequest(0, null), CancellationToken.None)).Result);
        Assert.Equal(1, await context.UserReviews.CountAsync());
    }

    private static async Task<(AppUser Seller, AppUser Buyer, Category Category, Listing Listing, Conversation Conversation)> CreateConversation(Bricker.Api.Data.BrickerDbContext context)
    {
        var seller = TestSupport.User("vendedor");
        var buyer = TestSupport.User("comprador");
        var category = TestSupport.Category();
        var listing = TestSupport.Listing(seller, category);
        var interest = new ListingInterest { ListingId = listing.Id, Listing = listing, InterestedUserId = buyer.Id, InterestedUser = buyer };
        var conversation = new Conversation { ListingId = listing.Id, Listing = listing, ListingInterestId = interest.Id, ListingInterest = interest, BuyerId = buyer.Id, SellerId = seller.Id };
        context.AddRange(seller, buyer, category, listing, interest, conversation);
        await context.SaveChangesAsync();
        return (seller, buyer, category, listing, conversation);
    }
}
