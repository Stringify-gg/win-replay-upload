using StringifyDesktop.Services;

namespace StringifyDesktop.Tests;

public sealed class ReplayWatcherServiceTests
{
    [Fact]
    public async Task WaitForReadyAsync_ReturnsTrueForStableFile()
    {
        var tempRoot = CreateTempDirectory();
        var filePath = Path.Combine(tempRoot, "match.replay");
        await File.WriteAllTextAsync(filePath, "replay-bytes");

        try
        {
            await using var watcher = new ReplayWatcherService(
                settleDelay: TimeSpan.FromMilliseconds(10),
                stabilityPollInterval: TimeSpan.FromMilliseconds(10),
                maxStabilityChecks: 2);

            var ready = await watcher.WaitForReadyAsync(filePath);

            Assert.True(ready);
        }
        finally
        {
            Directory.Delete(tempRoot, true);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"StringifyDesktop.Tests.{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }
}
