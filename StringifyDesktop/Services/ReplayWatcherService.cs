using System.Collections.Concurrent;

namespace StringifyDesktop.Services;

public sealed class ReplayWatcherService : IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, CancellationTokenSource> pending = new(StringComparer.OrdinalIgnoreCase);
    private readonly TimeSpan settleDelay;
    private readonly TimeSpan stabilityPollInterval;
    private readonly int maxStabilityChecks;
    private FileSystemWatcher? watcher;

    public ReplayWatcherService(
        TimeSpan? settleDelay = null,
        TimeSpan? stabilityPollInterval = null,
        int maxStabilityChecks = 4)
    {
        this.settleDelay = settleDelay ?? TimeSpan.FromSeconds(5);
        this.stabilityPollInterval = stabilityPollInterval ?? TimeSpan.FromMilliseconds(500);
        this.maxStabilityChecks = maxStabilityChecks;
    }

    public event Func<string, Task>? ReplayReady;

    public async Task<(bool Watching, string Directory)> StartAsync(string directory)
    {
        await StopAsync();

        watcher = new FileSystemWatcher(directory)
        {
            Filter = "*.replay",
            IncludeSubdirectories = false,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size
        };

        watcher.Created += OnFileChanged;
        watcher.Changed += OnFileChanged;
        watcher.Renamed += OnFileRenamed;
        watcher.EnableRaisingEvents = true;
        return (true, directory);
    }

    public Task StopAsync()
    {
        if (watcher is null)
        {
            return Task.CompletedTask;
        }

        watcher.EnableRaisingEvents = false;
        watcher.Created -= OnFileChanged;
        watcher.Changed -= OnFileChanged;
        watcher.Renamed -= OnFileRenamed;
        watcher.Dispose();
        watcher = null;

        foreach (var pair in pending)
        {
            pair.Value.Cancel();
            pair.Value.Dispose();
        }

        pending.Clear();
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<string>> ScanAsync(string directory)
    {
        try
        {
            var files = Directory
                .EnumerateFiles(directory, "*.replay", SearchOption.TopDirectoryOnly)
                .Where(File.Exists)
                .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            return Task.FromResult<IReadOnlyList<string>>(files);
        }
        catch
        {
            return Task.FromResult<IReadOnlyList<string>>([]);
        }
    }

    public async Task<bool> WaitForReadyAsync(string path, CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; attempt < maxStabilityChecks; attempt += 1)
        {
            if (!TryReadSnapshot(path, out var first))
            {
                await Task.Delay(stabilityPollInterval, cancellationToken);
                continue;
            }

            await Task.Delay(stabilityPollInterval, cancellationToken);

            if (!TryReadSnapshot(path, out var second))
            {
                continue;
            }

            if (first == second && CanOpenExclusively(path))
            {
                return true;
            }
        }

        return false;
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        Schedule(e.FullPath);
    }

    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        Schedule(e.FullPath);
    }

    private void Schedule(string path)
    {
        if (!path.EndsWith(".replay", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var cts = new CancellationTokenSource();
        pending.AddOrUpdate(
            path,
            static (_, state) => state!,
            static (_, existing, state) =>
            {
                existing.Cancel();
                existing.Dispose();
                return state!;
            },
            cts);

        _ = ProcessCandidateAsync(path, cts.Token);
    }

    private async Task ProcessCandidateAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(settleDelay, cancellationToken);
            if (await WaitForReadyAsync(path, cancellationToken) && ReplayReady is not null)
            {
                await ReplayReady.Invoke(path);
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            if (pending.TryRemove(path, out var removed))
            {
                removed.Dispose();
            }
        }
    }

    private static bool TryReadSnapshot(string path, out (long Length, DateTime LastWriteUtc) snapshot)
    {
        snapshot = default;
        try
        {
            var info = new FileInfo(path);
            if (!info.Exists)
            {
                return false;
            }

            snapshot = (info.Length, info.LastWriteTimeUtc);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool CanOpenExclusively(string path)
    {
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);
            return stream.Length >= 0;
        }
        catch
        {
            return false;
        }
    }
}
