namespace StringifyDesktop.Models;

public sealed record AppConfiguration(
    string BackendUrl,
    string OAuthIssuer,
    string OAuthClientId,
    string OAuthScopes,
    string OAuthCallbackUri,
    string DefaultReplayFolder);
