using Bricker.Api.Contracts;
using Bricker.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Bricker.Api.Validation;

namespace Bricker.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/profile")]
public sealed class ProfileController(UserManager<AppUser> userManager) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ProfileResponse>> Get(CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        return user is null ? Unauthorized() : Ok(ToResponse(user));
    }

    [HttpPut]
    public async Task<ActionResult<ProfileResponse>> Update(UpdateProfileRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DisplayName) || request.DisplayName.Trim().Length is < 2 or > 100)
            ModelState.AddModelError(nameof(request.DisplayName), "O nome deve ter entre 2 e 100 caracteres.");
        if (!string.IsNullOrWhiteSpace(request.City) && request.City.Trim().Length > 100)
            ModelState.AddModelError(nameof(request.City), "A cidade deve ter no máximo 100 caracteres.");
        if (!string.IsNullOrWhiteSpace(request.State) && !InputValidation.IsValidState(request.State))
            ModelState.AddModelError(nameof(request.State), "Informe a UF com duas letras.");
        if (!string.IsNullOrWhiteSpace(request.WhatsApp) && !InputValidation.IsValidWhatsApp(request.WhatsApp))
            ModelState.AddModelError(nameof(request.WhatsApp), "Informe um WhatsApp com DDD e 10 ou 11 números.");
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var user = await userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        user.DisplayName = request.DisplayName.Trim();
        user.City = string.IsNullOrWhiteSpace(request.City) ? null : request.City.Trim();
        user.State = string.IsNullOrWhiteSpace(request.State) ? null : request.State.Trim().ToUpperInvariant();
        user.WhatsApp = string.IsNullOrWhiteSpace(request.WhatsApp) ? null : InputValidation.Digits(request.WhatsApp);
        var result = await userManager.UpdateAsync(user);

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(error.Code, error.Description);
        }

        return result.Succeeded ? Ok(ToResponse(user)) : ValidationProblem(ModelState);
    }

    private static ProfileResponse ToResponse(AppUser user) => new(user.Id, user.DisplayName, user.Email!, user.City, user.State, user.WhatsApp, user.CreatedAtUtc);
}
