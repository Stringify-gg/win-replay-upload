namespace StringifyDesktop.Services;

public interface IUploadRoutingClient
{
    Task<(string UploadUrl, string Method, IReadOnlyDictionary<string, string> Headers)> RequestUploadUrlAsync(
        string fileName,
        long fileSize,
        string bearerToken,
        CancellationToken cancellationToken = default);
}
