using Bricker.Api.Controllers;
using Bricker.Api.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Bricker.Api.Tests;

public sealed class AuthControllerTests
{
    [Fact]
    public async Task Register_rejects_invalid_email_password_and_phone_before_creating_an_account()
    {
        var controller = new AuthController(null!, null!, new ConfigurationBuilder().Build());

        var result = await controller.Register(new RegisterRequest("A", "email inválido", "123", "Brusque", "S", "123"));

        Assert.IsType<ObjectResult>(result.Result);
        Assert.False(controller.ModelState.IsValid);
    }

    [Fact]
    public void Google_returns_service_unavailable_when_credentials_are_missing()
    {
        var controller = new AuthController(null!, null!, new ConfigurationBuilder().Build());

        var result = controller.Google("/");

        var problem = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, problem.StatusCode);
    }
}
