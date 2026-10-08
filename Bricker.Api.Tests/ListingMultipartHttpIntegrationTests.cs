using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Bricker.Api.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Bricker.Api.Tests;

public sealed class ListingMultipartHttpIntegrationTests : IClassFixture<SqlServerTestDatabase>
{
    private readonly SqlServerTestDatabase database;

    public ListingMultipartHttpIntegrationTests(SqlServerTestDatabase database) => this.database = database;

    [Fact]
    public async Task Create_listing_from_multipart_preserves_price_with_dot_decimal_separator()
    {
        var category = TestSupport.Category();
        await using (var setup = database.CreateContext())
        {
            setup.Categories.Add(category);
            await setup.SaveChangesAsync();
        }

        await using var factory = new BrickerWebApplicationFactory(database.ConnectionString);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = true
        });
        var email = $"http-{Guid.NewGuid():N}@test.bricker";
        var registration = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            displayName = "Comprador HTTP",
            email,
            password = "Bricker123",
            city = "Itajaí",
            state = "SC",
            whatsApp = (string?)null
        });
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);

        using var form = ListingForm(category.Id, "12.50", includeImage: true);
        var response = await client.PostAsync("/api/v1/listings", form);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        await using var assertion = database.CreateContext();
        var listing = await assertion.Listings.SingleAsync(item => item.Title == "Revestimento HTTP decimal");
        Assert.Equal(12.50m, listing.Price);
    }

    private static MultipartFormDataContent ListingForm(Guid categoryId, string price, bool includeImage)
    {
        var form = new MultipartFormDataContent();
        form.Add(new StringContent(categoryId.ToString()), "CategoryId");
        form.Add(new StringContent("Revestimento HTTP decimal"), "Title");
        form.Add(new StringContent("Material excedente bem conservado para uma nova obra."), "Description");
        form.Add(new StringContent(price), "Price");
        form.Add(new StringContent("unidade"), "Unit");
        form.Add(new StringContent("20"), "Quantity");
        form.Add(new StringContent(((int)MaterialCondition.Excellent).ToString()), "Condition");
        form.Add(new StringContent("Itajaí"), "City");
        form.Add(new StringContent("SC"), "State");
        form.Add(new StringContent("88300000"), "PostalCode");
        form.Add(new StringContent("Rua das Flores"), "Street");
        form.Add(new StringContent("Centro"), "Neighborhood");
        form.Add(new StringContent("10"), "AddressNumber");
        if (includeImage)
        {
            var image = new ByteArrayContent([1, 2, 3]);
            image.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            form.Add(image, "Images", "amostra.jpg");
        }
        return form;
    }

    private sealed class BrickerWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string? previousConnectionString;
        private readonly string? previousUploadsPath;
        private readonly string? previousKeysPath;
        private readonly string connectionString;
        private readonly string temporaryRoot;

        public BrickerWebApplicationFactory(string connectionString)
        {
            this.connectionString = connectionString;
            temporaryRoot = Path.Combine(Path.GetTempPath(), "bricker-http-tests", Guid.NewGuid().ToString("N"));
            previousConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__BrickerDb");
            previousUploadsPath = Environment.GetEnvironmentVariable("Storage__UploadsPath");
            previousKeysPath = Environment.GetEnvironmentVariable("DataProtection__KeysPath");
            Environment.SetEnvironmentVariable("ConnectionStrings__BrickerDb", connectionString);
            Environment.SetEnvironmentVariable("Storage__UploadsPath", Path.Combine(temporaryRoot, "uploads"));
            Environment.SetEnvironmentVariable("DataProtection__KeysPath", Path.Combine(temporaryRoot, "keys"));
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:BrickerDb"] = connectionString,
                ["Storage:UploadsPath"] = Path.Combine(temporaryRoot, "uploads"),
                ["DataProtection:KeysPath"] = Path.Combine(temporaryRoot, "keys"),
                ["DemoData:Seed"] = "false"
            }));
        }

        protected override void Dispose(bool disposing)
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__BrickerDb", previousConnectionString);
            Environment.SetEnvironmentVariable("Storage__UploadsPath", previousUploadsPath);
            Environment.SetEnvironmentVariable("DataProtection__KeysPath", previousKeysPath);
            if (Directory.Exists(temporaryRoot)) Directory.Delete(temporaryRoot, recursive: true);
            base.Dispose(disposing);
        }
    }
}
