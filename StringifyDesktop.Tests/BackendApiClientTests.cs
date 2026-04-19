using System.Net;
using System.Net.Http.Headers;
using System.Text;
using StringifyDesktop.Models;
using StringifyDesktop.Services;

namespace StringifyDesktop.Tests;

public sealed class BackendApiClientTests
{
    [Fact]
    public async Task RequestUploadUrlAsync_ParsesCamelCaseSignedUrlResponse()
    {
        HttpRequestMessage? capturedRequest = null;
        var httpClient = new HttpClient(new StubHttpHandler(request =>
        {
            capturedRequest = request;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                      "urls": [
                        {
                          "name": "match.replay",
                          "signedUrl": "https://storage.example/upload",
                          "method": "PUT",
                          "headers": {
                            "x-ms-blob-type": "BlockBlob"
                          }
                        }
                      ]
                    }
                    """,
                    Encoding.UTF8,
                    "application/json")
            };
        }));

        var client = new BackendApiClient(CreateConfiguration(), httpClient);

        var route = await client.RequestUploadUrlAsync("match.replay", 1234, "token");

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest!.Method);
        Assert.Equal("https://example.invalid/api/upload", capturedRequest.RequestUri!.ToString());
        Assert.Equal(new AuthenticationHeaderValue("Bearer", "token"), capturedRequest.Headers.Authorization);
        Assert.Equal("https://storage.example/upload", route.UploadUrl);
        Assert.Equal("PUT", route.Method);
        Assert.Equal("BlockBlob", route.Headers["x-ms-blob-type"]);
    }

    [Fact]
    public async Task RequestUploadUrlAsync_ParsesCamelCaseErrorResponse()
    {
        var httpClient = new HttpClient(new StubHttpHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(
                    """
                    {
                      "error": "bad payload"
                    }
                    """,
                    Encoding.UTF8,
                    "application/json")
            }));

        var client = new BackendApiClient(CreateConfiguration(), httpClient);

        var error = await Assert.ThrowsAsync<BackendError>(() =>
            client.RequestUploadUrlAsync("match.replay", 1234, "token"));

        Assert.Equal(400, error.Status);
        Assert.Equal("bad payload", error.Message);
    }

    private static AppConfiguration CreateConfiguration()
    {
        return new AppConfiguration(
            "https://example.invalid",
            "https://issuer.invalid",
            "client-id",
            "openid profile email",
            "stringify-gg://auth/callback",
            "C:\\Replays");
    }
}
