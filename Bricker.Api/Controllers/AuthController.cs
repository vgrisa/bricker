using Bricker.Api.Contracts;
using Bricker.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Bricker.Api.Validation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using System.Security.Claims;

namespace Bricker.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, IConfiguration configuration) : ControllerBase
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

    [HttpGet("google")]
    public IActionResult Google([FromQuery] string? returnUrl = "/")
    {
        if (string.IsNullOrWhiteSpace(configuration["Authentication:Google:ClientId"]) ||
            string.IsNullOrWhiteSpace(configuration["Authentication:Google:ClientSecret"]))
            return Problem("O login Google ainda não foi configurado.", statusCode: StatusCodes.Status503ServiceUnavailable);

        var safeReturnUrl = SafeReturnUrl(returnUrl);
        var callbackUrl = Url.Action(nameof(GoogleCallback), new { returnUrl = safeReturnUrl });
        var properties = signInManager.ConfigureExternalAuthenticationProperties(GoogleDefaults.AuthenticationScheme, callbackUrl);
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("google/callback")]
    public async Task<IActionResult> GoogleCallback([FromQuery] string? returnUrl = "/", [FromQuery] string? remoteError = null)
    {
        var frontendUrl = configuration["Frontend:BaseUrl"]?.TrimEnd('/') ?? "http://localhost:5173";
        if (!string.IsNullOrWhiteSpace(remoteError)) return Redirect($"{frontendUrl}/entrar?googleError=access_denied");

        var info = await signInManager.GetExternalLoginInfoAsync();
        if (info is null) return Redirect($"{frontendUrl}/entrar?googleError=invalid_callback");

        var verifiedClaim = info.Principal.FindFirstValue("urn:google:email_verified");
        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        var googleHostedEmail = !string.IsNullOrWhiteSpace(email) &&
            (email.EndsWith("@gmail.com", StringComparison.OrdinalIgnoreCase) ||
             !string.IsNullOrWhiteSpace(info.Principal.FindFirstValue("urn:google:hosted_domain")));
        var verifiedEmail = string.Equals(verifiedClaim, "true", StringComparison.OrdinalIgnoreCase) || googleHostedEmail;
        if (!verifiedEmail || string.IsNullOrWhiteSpace(email))
            return Redirect($"{frontendUrl}/entrar?googleError=email_not_verified");

        var user = await userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
        if (user is null)
        {
            user = await userManager.FindByEmailAsync(email);
            if (user is null)
            {
                var displayName = info.Principal.FindFirstValue(ClaimTypes.Name)?.Trim();
                user = new AppUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    DisplayName = string.IsNullOrWhiteSpace(displayName) ? email.Split('@')[0] : displayName,
                    RequiresProfileCompletion = true
                };
                var created = await userManager.CreateAsync(user);
                if (!created.Succeeded) return Redirect($"{frontendUrl}/entrar?googleError=create_failed");
            }

            var linked = await userManager.AddLoginAsync(user, info);
            if (!linked.Succeeded) return Redirect($"{frontendUrl}/entrar?googleError=link_failed");
        }

        await signInManager.SignInAsync(user, isPersistent: false);
        var destination = user.RequiresProfileCompletion ? "/completar-perfil" : SafeReturnUrl(returnUrl);
        return Redirect($"{frontendUrl}{destination}");
    }

    private static string SafeReturnUrl(string? returnUrl) =>
        !string.IsNullOrWhiteSpace(returnUrl) && returnUrl.StartsWith('/') && !returnUrl.StartsWith("//") ? returnUrl : "/";

    private static ProfileResponse ToResponse(AppUser user) => new(user.Id, user.DisplayName, user.Email!, user.City, user.State, user.WhatsApp, user.RequiresProfileCompletion, user.CreatedAtUtc);
}
