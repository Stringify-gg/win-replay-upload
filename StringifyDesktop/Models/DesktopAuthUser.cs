namespace StringifyDesktop.Models;

public sealed record DesktopAuthUser(
    string Sub,
    string? Email,
    bool? EmailVerified,
    string? Name,
    string? PreferredUsername,
    string? Picture);
