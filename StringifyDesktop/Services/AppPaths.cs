using Avalonia.Platform;

namespace StringifyDesktop.Services;

public sealed class AppPaths
{
    public string AppDataDirectory { get; }
    public bool AllowsLegacyImport { get; }
    public string SettingsPath => Path.Combine(AppDataDirectory, "settings.json");
    public string UploadLogPath => Path.Combine(AppDataDirectory, "upload-log.json");
    public string SessionPath => Path.Combine(AppDataDirectory, "session.dat");
    public string PendingFlowPath => Path.Combine(AppDataDirectory, "pending-auth-flow.json");
    public string DownloadsDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "Downloads");

    public AppPaths(string? appDataDirectory = null)
    {
        AllowsLegacyImport = string.IsNullOrWhiteSpace(appDataDirectory);
        AppDataDirectory = appDataDirectory
            ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "StringifyDesktop");
        Directory.CreateDirectory(AppDataDirectory);
    }

    public IEnumerable<string> EnumerateLegacyCandidates(string fileName)
    {
        var roots = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
        };

        var folders = new[]
        {
            "gg.strinova.app",
            "Stringify Desktop",
            "StringifyDesktop"
        };

        foreach (var root in roots.Where(static x => !string.IsNullOrWhiteSpace(x)))
        {
            foreach (var folder in folders)
            {
                yield return Path.Combine(root, folder, fileName);
            }
        }
    }

    public Stream OpenAppIconStream()
    {
        return AssetLoader.Open(new Uri("avares://StringifyDesktop/Assets/stringify.ico"));
    }
}
