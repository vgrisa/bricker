namespace Bricker.Api.Contracts;

public sealed record ProfileResponse(string Id, string DisplayName, string Email, string? City, string? State);

public sealed record UpdateProfileRequest(string DisplayName, string? City, string? State);
