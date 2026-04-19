namespace StringifyDesktop.Models;

public sealed record PendingAuthFlow(
    string State,
    string Nonce,
    string CodeVerifier,
    DateTimeOffset CreatedAt);
