using Bricker.Api.Contracts;
using Bricker.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Bricker.Api.Validation;

namespace Bricker.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<ProfileResponse>> Register(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DisplayName) || request.DisplayName.Trim().Length is < 2 or > 100)
            ModelState.AddModelError(nameof(request.DisplayName), "O nome deve ter entre 2 e 100 caracteres.");
        if (!InputValidation.IsValidEmail(request.Email))
            ModelState.AddModelError(nameof(request.Email), "Informe um e-mail válido.");
        if (string.IsNullOrEmpty(request.Password) || request.Password.Length is < 8 or > 128)
            ModelState.AddModelError(nameof(request.Password), "A senha deve ter entre 8 e 128 caracteres.");
        if (!string.IsNullOrWhiteSpace(request.City) && request.City.Trim().Length > 100)
            ModelState.AddModelError(nameof(request.City), "A cidade deve ter no máximo 100 caracteres.");
        if (!string.IsNullOrWhiteSpace(request.State) && !InputValidation.IsValidState(request.State))
            ModelState.AddModelError(nameof(request.State), "Informe a UF com duas letras.");
        if (!string.IsNullOrWhiteSpace(request.WhatsApp) && !InputValidation.IsValidWhatsApp(request.WhatsApp))
            ModelState.AddModelError(nameof(request.WhatsApp), "Informe um WhatsApp com DDD e 10 ou 11 números.");

        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var user = new AppUser
        {
            UserName = request.Email.Trim(),
            Email = request.Email.Trim(),
            DisplayName = request.DisplayName.Trim(),
            City = string.IsNullOrWhiteSpace(request.City) ? null : request.City.Trim(),
            State = string.IsNullOrWhiteSpace(request.State) ? null : request.State.Trim().ToUpperInvariant(),
            WhatsApp = string.IsNullOrWhiteSpace(request.WhatsApp) ? null : InputValidation.Digits(request.WhatsApp)
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors) ModelState.AddModelError(error.Code, error.Description);
            return ValidationProblem(ModelState);
        }

        await signInManager.SignInAsync(user, isPersistent: false);
        return Created("/api/v1/profile", ToResponse(user));
    }

    [HttpPost("login")]
    public async Task<ActionResult<ProfileResponse>> Login(LoginRequest request)
    {
        if (!InputValidation.IsValidEmail(request.Email) || string.IsNullOrEmpty(request.Password) || request.Password.Length > 128)
            return Unauthorized(new { message = "E-mail ou senha inválidos." });
        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
        {
            return Unauthorized(new { message = "E-mail ou senha inválidos." });
        }

        await signInManager.SignInAsync(user, isPersistent: false);
        return Ok(ToResponse(user));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return NoContent();
    }

    private static ProfileResponse ToResponse(AppUser user) => new(user.Id, user.DisplayName, user.Email!, user.City, user.State, user.WhatsApp, user.CreatedAtUtc);
}
