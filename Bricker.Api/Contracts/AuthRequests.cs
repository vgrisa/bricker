namespace Bricker.Api.Contracts;

public sealed record RegisterRequest(string DisplayName, string Email, string Password, string? City, string? State, string? WhatsApp);

public sealed record LoginRequest(string Email, string Password);
