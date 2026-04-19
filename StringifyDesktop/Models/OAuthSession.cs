namespace StringifyDesktop.Models;

public sealed record OAuthSession(
    string AccessToken,
    string? RefreshToken,
    string? IdToken,
    string TokenType,
    string Scope,
    DateTimeOffset ExpiresAt,
    DesktopAuthUser User);
