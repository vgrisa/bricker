using Bricker.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Bricker.Api.Storage;

namespace Bricker.Api.Data;

public static class DevelopmentDataSeeder
{
    public const string DemoPassword = "Bricker123";

    private const string AnaId = "demo-user-ana";
    private const string CarlosId = "demo-user-carlos";
    private const string MarinaId = "demo-user-marina";

    public static async Task SeedAsync(IServiceProvider services, string contentRootPath, CancellationToken cancellationToken = default)
    {
        var db = services.GetRequiredService<BrickerDbContext>();
        var userManager = services.GetRequiredService<UserManager<AppUser>>();

        services.GetRequiredService<UploadStorage>().CopyDemoAssets(contentRootPath);

        var demoUsers = await db.Users.CountAsync(user =>
            user.Id == AnaId || user.Id == CarlosId || user.Id == MarinaId, cancellationToken);
        if (demoUsers == 3) return;
        if (demoUsers != 0)
            throw new InvalidOperationException("Os dados de demonstração estão incompletos. Recrie o banco antes de continuar.");

        // The migrations contain three historical catalog examples without owners.
        // On a fresh development database, replace them with complete scenarios.
        var historicalListings = await db.Listings
            .Where(listing => listing.SellerId == null)
            .ToListAsync(cancellationToken);
        db.Listings.RemoveRange(historicalListings);
        await db.SaveChangesAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var ana = new AppUser
        {
            Id = AnaId,
            UserName = "ana@demo.bricker.com.br",
            Email = "ana@demo.bricker.com.br",
            EmailConfirmed = true,
            DisplayName = "Ana Souza",
            City = "Itajaí",
            State = "SC",
            WhatsApp = "47991234567",
            CreatedAtUtc = now.AddMonths(-10)
        };
        var carlos = new AppUser
        {
            Id = CarlosId,
            UserName = "carlos@demo.bricker.com.br",
            Email = "carlos@demo.bricker.com.br",
            EmailConfirmed = true,
            DisplayName = "Carlos Mendes",
            City = "Brusque",
            State = "SC",
            WhatsApp = "47992345678",
            CreatedAtUtc = now.AddMonths(-8)
        };
        var marina = new AppUser
        {
            Id = MarinaId,
            UserName = "marina@demo.bricker.com.br",
            Email = "marina@demo.bricker.com.br",
            EmailConfirmed = true,
            DisplayName = "Marina Oliveira",
            City = "Balneário Camboriú",
            State = "SC",
            WhatsApp = "47993456789",
            CreatedAtUtc = now.AddMonths(-5)
        };

        await CreateUser(userManager, ana);
        await CreateUser(userManager, carlos);
        await CreateUser(userManager, marina);

        var listings = new[]
        {
            CreateListing(
                Guid.Parse("d1000000-0000-0000-0000-000000000001"), SeedIds.Revestimentos, ana,
                "Porcelanato acetinado cinza 60 × 60", "Sobraram sete caixas fechadas e algumas peças avulsas da reforma. O porcelanato está novo, seco e armazenado em área coberta.",
                44.90m, "m²", 18.5m, MaterialCondition.Excellent, "Itajaí", "SC", "88301000", "Rua Brusque", "Centro", "640", null,
                "porcelanato", now.AddDays(-2)),
            CreateListing(
                Guid.Parse("d1000000-0000-0000-0000-000000000002"), SeedIds.Madeira, ana,
                "Portas internas de madeira maciça", "Três portas novas de madeira maciça, sem ferragens e nunca instaladas. Medidas de 80 × 210 cm, prontas para retirada.",
                395m, "unidade", 3m, MaterialCondition.Excellent, "Itajaí", "SC", "88305401", "Rua José Pereira Liberato", "São João", "1250", "Galpão 2",
                "portas", now.AddDays(-6)),
            CreateListing(
                Guid.Parse("d1000000-0000-0000-0000-000000000003"), SeedIds.Revestimentos, carlos,
                "Tijolos cerâmicos de 6 furos", "Lote excedente de uma obra residencial. Os tijolos estão paletizados, em bom estado e disponíveis para retirada no local.",
                1.35m, "unidade", 1200m, MaterialCondition.Good, "Brusque", "SC", "88350100", "Rua Azambuja", "Azambuja", "830", null,
                "tijolos", now.AddDays(-1)),
            CreateListing(
                Guid.Parse("d1000000-0000-0000-0000-000000000004"), SeedIds.Hidraulica, carlos,
                "Tubos e conexões de PVC", "Conjunto com tubos de diferentes diâmetros, joelhos e conexões que sobraram de instalações hidráulicas. Venda preferencial do lote completo.",
                620m, "lote", 1m, MaterialCondition.Good, "Brusque", "SC", "88353200", "Rua Primeiro de Maio", "Águas Claras", "312", null,
                "tubos", now.AddDays(-9)),
            CreateListing(
                Guid.Parse("d1000000-0000-0000-0000-000000000005"), SeedIds.Eletrica, marina,
                "Rolos de cabo elétrico 2,5 mm²", "Quatro rolos parcialmente utilizados, todos identificados e armazenados em local seco. Aproximadamente 70 metros por rolo.",
                185m, "rolo", 4m, MaterialCondition.Excellent, "Balneário Camboriú", "SC", "88330025", "Rua 1500", "Centro", "425", "Sala térrea",
                "cabos", now.AddDays(-4)),
            CreateListing(
                Guid.Parse("d1000000-0000-0000-0000-000000000006"), SeedIds.Ferragens, marina,
                "Vergalhões de aço CA-50", "Barras de 10 mm que não foram utilizadas na fundação. Estão organizadas em feixes e apresentam apenas oxidação superficial de armazenamento.",
                58m, "barra", 42m, MaterialCondition.Good, "Balneário Camboriú", "SC", "88339005", "Avenida Marginal Oeste", "Municípios", "1550", null,
                "vergalhoes", now.AddDays(-12)),
            CreateListing(
                Guid.Parse("d1000000-0000-0000-0000-000000000007"), SeedIds.Eletrica, carlos,
                "Lote de cabos para instalação residencial", "Lote de cabos elétricos vendido pela Bricker. O anúncio permanece no perfil para demonstrar o histórico da negociação.",
                740m, "lote", 1m, MaterialCondition.Good, "Brusque", "SC", "88350001", "Rua Hercílio Luz", "Centro", "91", null,
                "cabos", now.AddDays(-28), ListingStatus.Sold)
        };
        db.Listings.AddRange(listings);

        var porcelainInterest = new ListingInterest
        {
            Id = Guid.Parse("d2000000-0000-0000-0000-000000000001"),
            ListingId = listings[0].Id,
            InterestedUserId = CarlosId,
            CreatedAtUtc = now.AddDays(-1).AddHours(-5)
        };
        var doorsInterest = new ListingInterest
        {
            Id = Guid.Parse("d2000000-0000-0000-0000-000000000002"),
            ListingId = listings[1].Id,
            InterestedUserId = MarinaId,
            CreatedAtUtc = now.AddDays(-3)
        };
        var soldInterest = new ListingInterest
        {
            Id = Guid.Parse("d2000000-0000-0000-0000-000000000003"),
            ListingId = listings[6].Id,
            InterestedUserId = AnaId,
            CreatedAtUtc = now.AddDays(-25)
        };
        db.ListingInterests.AddRange(porcelainInterest, doorsInterest, soldInterest);

        var porcelainConversation = CreateConversation(
            Guid.Parse("d3000000-0000-0000-0000-000000000001"), porcelainInterest, listings[0], carlos, ana,
            now.AddDays(-1).AddHours(-5));
        AddMessage(porcelainConversation, CarlosId, "Olá, Ana! As 18,5 m² ainda estão disponíveis?", now.AddDays(-1).AddHours(-5), now.AddDays(-1).AddHours(-4));
        AddMessage(porcelainConversation, AnaId, "Oi, Carlos! Sim, o lote está completo e pode ser retirado esta semana.", now.AddDays(-1).AddHours(-4), now.AddHours(-3));
        AddMessage(porcelainConversation, CarlosId, "Ótimo. Consigo passar amanhã no fim da tarde para conferir?", now.AddHours(-3), null);

        var doorsConversation = CreateConversation(
            Guid.Parse("d3000000-0000-0000-0000-000000000002"), doorsInterest, listings[1], marina, ana,
            now.AddDays(-3));
        AddMessage(doorsConversation, MarinaId, "Boa tarde! As três portas têm a mesma medida?", now.AddDays(-3), now.AddDays(-2));
        AddMessage(doorsConversation, AnaId, "Boa tarde! Sim, todas medem 80 por 210 cm.", now.AddDays(-2), null);

        var soldConversation = CreateConversation(
            Guid.Parse("d3000000-0000-0000-0000-000000000003"), soldInterest, listings[6], ana, carlos,
            now.AddDays(-25));
        AddMessage(soldConversation, AnaId, "Tenho interesse no lote completo. Ele está separado por bitola?", now.AddDays(-25), now.AddDays(-24));
        AddMessage(soldConversation, CarlosId, "Está sim. Posso deixar tudo pronto para retirada no sábado.", now.AddDays(-24), now.AddDays(-23));
        AddMessage(soldConversation, AnaId, "Combinado, obrigada!", now.AddDays(-23), now.AddDays(-23));
        AddSystemMessage(soldConversation, "Este material foi vendido.", now.AddDays(-20));
        db.Conversations.AddRange(porcelainConversation, doorsConversation, soldConversation);

        db.ListingFavorites.AddRange(
            new ListingFavorite { ListingId = listings[0].Id, UserId = MarinaId, CreatedAtUtc = now.AddDays(-1) },
            new ListingFavorite { ListingId = listings[2].Id, UserId = AnaId, CreatedAtUtc = now.AddHours(-8) },
            new ListingFavorite { ListingId = listings[5].Id, UserId = CarlosId, CreatedAtUtc = now.AddDays(-2) });

        var sale = new ListingSale
        {
            Id = Guid.Parse("d4000000-0000-0000-0000-000000000001"),
            ListingId = listings[6].Id,
            ListingInterestId = soldInterest.Id,
            BuyerId = AnaId,
            SellerId = CarlosId,
            ConfirmedAtUtc = now.AddDays(-20)
        };
        db.ListingSales.Add(sale);
        db.UserReviews.Add(new UserReview
        {
            Id = Guid.Parse("d5000000-0000-0000-0000-000000000001"),
            ListingSaleId = sale.Id,
            ReviewerId = AnaId,
            RevieweeId = CarlosId,
            Rating = 5,
            Comment = "Material conforme o anúncio e retirada bem organizada. Negociação tranquila!",
            CreatedAtUtc = now.AddDays(-18)
        });

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task CreateUser(UserManager<AppUser> userManager, AppUser user)
    {
        var result = await userManager.CreateAsync(user, DemoPassword);
        if (!result.Succeeded)
            throw new InvalidOperationException($"Não foi possível criar {user.Email}: {string.Join("; ", result.Errors.Select(error => error.Description))}");
    }

    private static Listing CreateListing(
        Guid id, Guid categoryId, AppUser seller, string title, string description, decimal price, string unit,
        decimal quantity, MaterialCondition condition, string city, string state, string postalCode, string street,
        string neighborhood, string number, string? complement, string imageSlug, DateTime createdAt,
        ListingStatus status = ListingStatus.Active)
    {
        var listing = new Listing
        {
            Id = id,
            CategoryId = categoryId,
            SellerId = seller.Id,
            SellerDisplayName = seller.DisplayName,
            Title = title,
            Description = description,
            Price = price,
            Unit = unit,
            Quantity = quantity,
            Condition = condition,
            Status = status,
            City = city,
            State = state,
            PostalCode = postalCode,
            Street = street,
            Neighborhood = neighborhood,
            AddressNumber = number,
            AddressComplement = complement,
            ImageUrl = $"/uploads/demo/{imageSlug}-1.webp",
            CreatedAtUtc = createdAt
        };
        listing.Images.Add(new ListingImage { ListingId = id, Url = $"/uploads/demo/{imageSlug}-1.webp", SortOrder = 0 });
        listing.Images.Add(new ListingImage { ListingId = id, Url = $"/uploads/demo/{imageSlug}-2.webp", SortOrder = 1 });
        return listing;
    }

    private static Conversation CreateConversation(Guid id, ListingInterest interest, Listing listing, AppUser buyer, AppUser seller, DateTime createdAt) =>
        new()
        {
            Id = id,
            ListingInterestId = interest.Id,
            ListingId = listing.Id,
            BuyerId = buyer.Id,
            SellerId = seller.Id,
            CreatedAtUtc = createdAt
        };

    private static void AddMessage(Conversation conversation, string senderId, string body, DateTime createdAt, DateTime? readAt)
    {
        conversation.Messages.Add(new ChatMessage
        {
            ConversationId = conversation.Id,
            SenderId = senderId,
            Body = body,
            Type = ChatMessageType.User,
            CreatedAtUtc = createdAt,
            ReadAtUtc = readAt
        });
        conversation.LastMessageAtUtc = createdAt;
    }

    private static void AddSystemMessage(Conversation conversation, string body, DateTime createdAt)
    {
        conversation.Messages.Add(new ChatMessage
        {
            ConversationId = conversation.Id,
            Body = body,
            Type = ChatMessageType.System,
            CreatedAtUtc = createdAt
        });
        conversation.LastMessageAtUtc = createdAt;
    }

}
