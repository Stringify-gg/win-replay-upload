using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using StringifyDesktop.Models;

namespace StringifyDesktop.Services;

public sealed class BackendApiClient : IUploadRoutingClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient client;
    private readonly AppConfiguration configuration;

    public BackendApiClient(AppConfiguration configuration, HttpClient? client = null)
    {
        this.configuration = configuration;
        this.client = client ?? new HttpClient();
    }

    public async Task<(string UploadUrl, string Method, IReadOnlyDictionary<string, string> Headers)> RequestUploadUrlAsync(
        string fileName,
        long fileSize,
        string bearerToken,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{configuration.BackendUrl}/api/upload");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        request.Content = new StringContent(
            JsonSerializer.Serialize(new
            {
                files = new[]
                {
                    new { name = fileName, size = fileSize }
                }
            }),
            Encoding.UTF8,
            "application/json");

        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new BackendError(await ReadErrorAsync(response), (int)response.StatusCode);
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var payload = await JsonSerializer.DeserializeAsync<UploadUrlsResponse>(stream, JsonOptions, cancellationToken);
        var first = payload?.Urls?.FirstOrDefault();
        if (first is null)
        {
            throw new BackendError("Upload API did not return a signed URL.", 502);
        }

        if (!string.IsNullOrWhiteSpace(first.Error))
        {
            throw new BackendError(first.Error, 400);
        }

        var uploadUrl = first.SignedUrl ?? first.UploadUrl ?? first.Url;
        if (string.IsNullOrWhiteSpace(uploadUrl))
        {
            throw new BackendError("Upload API did not return a signed URL.", 502);
        }

        return (
            uploadUrl,
            string.IsNullOrWhiteSpace(first.Method) ? "PUT" : first.Method,
            first.Headers ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response)
    {
        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync();
            var payload = await JsonSerializer.DeserializeAsync<ErrorResponse>(stream, JsonOptions);
            return payload?.Error ?? payload?.Message ?? $"{(int)response.StatusCode} {response.ReasonPhrase}";
        }
        catch
        {
            return $"{(int)response.StatusCode} {response.ReasonPhrase}";
        }
    }

    private sealed class UploadUrlsResponse
    {
        [JsonPropertyName("urls")]
        public List<UploadUrlEntry>? Urls { get; set; }
    }

    private sealed class UploadUrlEntry
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("signedUrl")]
        public string? SignedUrl { get; set; }

        [JsonPropertyName("uploadUrl")]
        public string? UploadUrl { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("method")]
        public string? Method { get; set; }

        [JsonPropertyName("headers")]
        public Dictionary<string, string>? Headers { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }

    private sealed class ErrorResponse
    {
        [JsonPropertyName("error")]
        public string? Error { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }
}
