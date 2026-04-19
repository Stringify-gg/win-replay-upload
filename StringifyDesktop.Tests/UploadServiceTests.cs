using System.Net;
using StringifyDesktop.Models;
using StringifyDesktop.Services;

namespace StringifyDesktop.Tests;

public sealed class UploadServiceTests
{
    [Fact]
    public async Task UploadReplayAsync_MapsBackend403ToAlreadyUploaded()
    {
        var tempRoot = CreateTempDirectory();
        var filePath = Path.Combine(tempRoot, "match.replay");
        await File.WriteAllBytesAsync(filePath, ReplayFileTestData.CreateReplayBytes(headerMagic: ReplayFileTestData.NetworkDemoMagic));

        try
        {
            var service = new UploadService(
                new FakeTokenSource("token"),
                new FakeUploadRoutingClient(_ => throw new BackendError("already there", 403)));

            var outcome = await service.UploadReplayAsync(filePath, "match.replay");

            var typed = Assert.IsType<UploadOutcome.AlreadyUploaded>(outcome);
            Assert.Equal(403, typed.HttpStatus);
        }
        finally
        {
            Directory.Delete(tempRoot, true);
        }
    }

    [Fact]
    public async Task UploadReplayAsync_MapsUpload403ToAlreadyUploaded()
    {
        var tempRoot = CreateTempDirectory();
        var filePath = Path.Combine(tempRoot, "match.replay");
        await File.WriteAllBytesAsync(filePath, ReplayFileTestData.CreateReplayBytes(headerMagic: ReplayFileTestData.NetworkDemoMagic));

        try
        {
            var uploadClient = new HttpClient(new StubHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.Forbidden)));
            var service = new UploadService(
                new FakeTokenSource("token"),
                new FakeUploadRoutingClient(_ => Task.FromResult<(string, string, IReadOnlyDictionary<string, string>)>(("https://example.invalid/upload", "PUT", new Dictionary<string, string>()))),
                uploadClient);

            var outcome = await service.UploadReplayAsync(filePath, "match.replay");

            var typed = Assert.IsType<UploadOutcome.AlreadyUploaded>(outcome);
            Assert.Equal(403, typed.HttpStatus);
        }
        finally
        {
            Directory.Delete(tempRoot, true);
        }
    }

    [Fact]
    public async Task UploadReplayAsync_FailsInvalidReplayBeforeRequestingUploadUrl()
    {
        var tempRoot = CreateTempDirectory();
        var filePath = Path.Combine(tempRoot, "match.replay");
        await File.WriteAllBytesAsync(filePath, [1, 2, 3, 4, 5, 6]);

        try
        {
            var requestCount = 0;
            var service = new UploadService(
                new FakeTokenSource("token"),
                new FakeUploadRoutingClient(_ =>
                {
                    requestCount += 1;
                    return Task.FromResult<(string, string, IReadOnlyDictionary<string, string>)>(("https://example.invalid/upload", "PUT", new Dictionary<string, string>()));
                }));

            var outcome = await service.UploadReplayAsync(filePath, "match.replay");

            var typed = Assert.IsType<UploadOutcome.Failed>(outcome);
            Assert.Contains("File too small", typed.Error, StringComparison.Ordinal);
            Assert.Equal(0, requestCount);
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

    private sealed class FakeTokenSource : IAccessTokenSource
    {
        private readonly string? token;

        public FakeTokenSource(string? token)
        {
            this.token = token;
        }

        public Task<string?> GetAccessTokenAsync()
        {
            return Task.FromResult(token);
        }
    }

    private sealed class FakeUploadRoutingClient : IUploadRoutingClient
    {
        private readonly Func<(string FileName, long Size, string Token), Task<(string, string, IReadOnlyDictionary<string, string>)>> handler;

        public FakeUploadRoutingClient(Func<(string FileName, long Size, string Token), Task<(string, string, IReadOnlyDictionary<string, string>)>> handler)
        {
            this.handler = handler;
        }

        public Task<(string UploadUrl, string Method, IReadOnlyDictionary<string, string> Headers)> RequestUploadUrlAsync(string fileName, long fileSize, string bearerToken, CancellationToken cancellationToken = default)
        {
            return handler((fileName, fileSize, bearerToken));
        }
    }
}
