using Bricker.Api.Controllers;
using Bricker.Api.Contracts;
using Bricker.Api.Validation;
using Microsoft.AspNetCore.Mvc;

namespace Bricker.Api.Tests;

public sealed class ProfileAndValidationTests : IClassFixture<SqlServerTestDatabase>
{
    private readonly SqlServerTestDatabase database;

    public ProfileAndValidationTests(SqlServerTestDatabase database) => this.database = database;

    [Theory]
    [InlineData("(47) 99999-9999", true)]
    [InlineData("479999999999999", false)]
    [InlineData("00000000000", false)]
    public void WhatsApp_validation_accepts_only_realistic_lengths(string value, bool expected) =>
        Assert.Equal(expected, InputValidation.IsValidWhatsApp(value));

    [Fact]
    public async Task Profile_update_normalizes_phone_state_and_completes_profile()
    {
        await using var context = database.CreateContext();
        var user = TestSupport.User("perfil");
        user.RequiresProfileCompletion = true;
        context.Users.Add(user);
        await context.SaveChangesAsync();
        using var store = new TestUserStore(context);
        var controller = new ProfileController(TestSupport.UserManager(store))
        {
            ControllerContext = new() { HttpContext = TestSupport.UserContext(user.Id) }
        };

        var result = await controller.Update(new UpdateProfileRequest("Novo nome", "Brusque", "sc", "(47) 99999-9999"));

        var response = Assert.IsType<OkObjectResult>(result.Result).Value as ProfileResponse;
        Assert.NotNull(response);
        Assert.Equal("SC", response.State);
        Assert.Equal("47999999999", response.WhatsApp);
        Assert.False(response.RequiresProfileCompletion);
    }

    [Fact]
    public async Task Profile_update_rejects_invalid_phone()
    {
        await using var context = database.CreateContext();
        var user = TestSupport.User("perfil");
        context.Users.Add(user);
        await context.SaveChangesAsync();
        using var store = new TestUserStore(context);
        var controller = new ProfileController(TestSupport.UserManager(store))
        {
            ControllerContext = new() { HttpContext = TestSupport.UserContext(user.Id) }
        };

        var result = await controller.Update(new UpdateProfileRequest("Nome válido", "Brusque", "SC", "123"));

        Assert.IsType<ObjectResult>(result.Result);
    }
}
