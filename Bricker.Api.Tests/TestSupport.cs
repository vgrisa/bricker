using System.Security.Claims;
using Bricker.Api.Hubs;
using Bricker.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Bricker.Api.Tests;

internal static class TestSupport
{
    public static AppUser User(string name) => new()
    {
        Id = Guid.NewGuid().ToString(),
        UserName = $"{name}-{Guid.NewGuid():N}@test.bricker",
        Email = $"{name}-{Guid.NewGuid():N}@test.bricker",
        DisplayName = name,
        City = "Brusque",
        State = "SC"
    };

    public static UserManager<AppUser> UserManager(TestUserStore store) => new(
        store, Options.Create(new IdentityOptions()), new PasswordHasher<AppUser>(), [], [],
        new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(), new ServiceCollection().BuildServiceProvider(),
        NullLogger<UserManager<AppUser>>.Instance);

    public static DefaultHttpContext UserContext(string userId) => new()
    {
        User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId)], "Test"))
    };

    public static IHubContext<ChatHub> Hub()
    {
        var proxy = new Mock<IClientProxy>();
        proxy.Setup(item => item.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var clients = new Mock<IHubClients>();
        clients.Setup(item => item.Group(It.IsAny<string>())).Returns(proxy.Object);
        clients.Setup(item => item.Groups(It.IsAny<IReadOnlyList<string>>())).Returns(proxy.Object);
        var hub = new Mock<IHubContext<ChatHub>>();
        hub.SetupGet(item => item.Clients).Returns(clients.Object);
        return hub.Object;
    }

    public static Category Category() => new() { Name = "Elétrica", Slug = $"eletrica-{Guid.NewGuid():N}" };

    public static Listing Listing(AppUser seller, Category category, ListingStatus status = ListingStatus.Active) => new()
    {
        Category = category,
        Title = "Cabos elétricos excedentes",
        Description = "Material em bom estado para aproveitar na sua obra.",
        Price = 120m,
        Unit = "rolo",
        Quantity = 2,
        Condition = MaterialCondition.Excellent,
        Status = status,
        City = "Brusque",
        State = "SC",
        PostalCode = "88350000",
        Street = "Rua Teste",
        Neighborhood = "Centro",
        AddressNumber = "10",
        SellerId = seller.Id,
        SellerDisplayName = seller.DisplayName
    };
}
