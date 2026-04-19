using System.Text.Json;
using StringifyDesktop.Models;

namespace StringifyDesktop.Services;

public sealed class UploadLogStore
{
    private readonly AppPaths paths;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly Dictionary<string, UploadLogEntry> entries = new(StringComparer.OrdinalIgnoreCase);

    public UploadLogStore(AppPaths paths)
    {
        this.paths = paths;
    }

    public async Task InitializeAsync()
    {
        if (paths.AllowsLegacyImport && !File.Exists(paths.UploadLogPath))
        {
            await TryImportLegacyAsync();
        }

        if (!File.Exists(paths.UploadLogPath))
        {
            return;
        }

        try
        {
            await using var stream = File.OpenRead(paths.UploadLogPath);
            var loaded = await JsonSerializer.DeserializeAsync<Dictionary<string, UploadLogEntry>>(stream, JsonOptions)
                ?? new Dictionary<string, UploadLogEntry>(StringComparer.OrdinalIgnoreCase);
            entries.Clear();
            foreach (var pair in loaded)
            {
                entries[pair.Key] = pair.Value;
            }
        }
        catch
        {
            entries.Clear();
        }
    }

    public IReadOnlyDictionary<string, UploadLogEntry> GetSnapshot()
    {
        return new Dictionary<string, UploadLogEntry>(entries, StringComparer.OrdinalIgnoreCase);
    }

    public UploadLogEntry? GetStatus(string fileName)
    {
        return entries.TryGetValue(fileName, out var entry) ? entry : null;
    }

    public async Task SetAsync(string fileName, UploadLogEntry entry)
    {
        entries[fileName] = entry;
        await PersistAsync();
    }

    public async Task<int> ClearFailedAsync()
    {
        var removed = 0;
        foreach (var key in entries.Where(static pair => pair.Value.Status == UploadStatus.Failed).Select(static pair => pair.Key).ToArray())
        {
            entries.Remove(key);
            removed += 1;
        }

        await PersistAsync();
        return removed;
    }

    public async Task<string> ExportAsync()
    {
        Directory.CreateDirectory(paths.DownloadsDirectory);
        var target = Path.Combine(paths.DownloadsDirectory, $"replay-upload-log-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.json");
        await using var stream = File.Create(target);
        await JsonSerializer.SerializeAsync(stream, entries, new JsonSerializerOptions(JsonOptions) { WriteIndented = true });
        return target;
    }

    private async Task PersistAsync()
    {
        await using var stream = File.Create(paths.UploadLogPath);
        await JsonSerializer.SerializeAsync(stream, entries, new JsonSerializerOptions(JsonOptions) { WriteIndented = true });
    }

    private async Task TryImportLegacyAsync()
    {
        foreach (var candidate in paths.EnumerateLegacyCandidates("upload-log.json"))
        {
            if (!File.Exists(candidate))
            {
                continue;
            }

            try
            {
                var text = await File.ReadAllTextAsync(candidate);
                await File.WriteAllTextAsync(paths.UploadLogPath, text);
                break;
            }
            catch
            {
                // Ignore broken legacy imports and continue.
            }
        }
    }
}
