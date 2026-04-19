using StringifyDesktop.Services;

namespace StringifyDesktop.Tests;

public sealed class ReplayFileValidatorTests
{
    [Fact]
    public async Task ValidateAsync_RejectsInvalidLocalMagic()
    {
        var tempRoot = CreateTempDirectory();
        var filePath = Path.Combine(tempRoot, "bad.replay");
        await File.WriteAllBytesAsync(
            filePath,
            ReplayFileTestData.CreateReplayBytes(
                localMagic: 0x01020304,
                headerMagic: ReplayFileTestData.NetworkDemoMagic));

        try
        {
            var validator = new ReplayFileValidator();

            var result = await validator.ValidateAsync(filePath);

            Assert.False(result.IsValid);
            Assert.Equal("Invalid replay file (magic 0x01020304)", result.Error);
        }
        finally
        {
            Directory.Delete(tempRoot, true);
        }
    }

    [Fact]
    public async Task ValidateAsync_RejectsInvalidHeaderChunkMagicWhenUncompressed()
    {
        var tempRoot = CreateTempDirectory();
        var filePath = Path.Combine(tempRoot, "bad-header.replay");
        await File.WriteAllBytesAsync(
            filePath,
            ReplayFileTestData.CreateReplayBytes(
                compressed: false,
                headerMagic: 0xdeadbeef));

        try
        {
            var validator = new ReplayFileValidator();

            var result = await validator.ValidateAsync(filePath);

            Assert.False(result.IsValid);
            Assert.Equal("Invalid header chunk magic (0xdeadbeef)", result.Error);
        }
        finally
        {
            Directory.Delete(tempRoot, true);
        }
    }

    [Fact]
    public async Task ValidateAsync_AllowsCompressedHeaderChunkWithoutExpectedMagic()
    {
        var tempRoot = CreateTempDirectory();
        var filePath = Path.Combine(tempRoot, "compressed.replay");
        await File.WriteAllBytesAsync(
            filePath,
            ReplayFileTestData.CreateReplayBytes(
                compressed: true,
                headerMagic: 0xdeadbeef));

        try
        {
            var validator = new ReplayFileValidator();

            var result = await validator.ValidateAsync(filePath);

            Assert.True(result.IsValid);
            Assert.Null(result.Error);
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
