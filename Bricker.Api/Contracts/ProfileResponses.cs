namespace Bricker.Api.Contracts;

public sealed record ProfileResponse(string Id, string DisplayName, string Email, string? City, string? State, string? WhatsApp, DateTime CreatedAtUtc);

public sealed record UpdateProfileRequest(string DisplayName, string? City, string? State, string? WhatsApp);
