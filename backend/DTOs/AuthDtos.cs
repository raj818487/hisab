namespace MilkHisab.Api.DTOs;

public sealed record AuthTokenResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt,
    string Name);

public sealed record RefreshRequest(string RefreshToken);