namespace StringifyDesktop.Models;

public sealed record AppSettings(bool AutoSyncEnabled, string? WatchDir)
{
    public static AppSettings Default { get; } = new(true, null);
}
