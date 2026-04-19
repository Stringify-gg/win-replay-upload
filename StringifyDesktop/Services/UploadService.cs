using StringifyDesktop.Models;

namespace StringifyDesktop.Services;

public sealed class UploadService
{
    private readonly IAccessTokenSource accessTokenSource;
    private readonly IUploadRoutingClient backendApiClient;
    private readonly HttpClient uploadClient;
    private readonly ReplayFileValidator replayFileValidator;

    public UploadService(
        IAccessTokenSource accessTokenSource,
        IUploadRoutingClient backendApiClient,
        HttpClient? uploadClient = null,
        ReplayFileValidator? replayFileValidator = null)
    {
        this.accessTokenSource = accessTokenSource;
        this.backendApiClient = backendApiClient;
        this.uploadClient = uploadClient ?? new HttpClient();
        this.replayFileValidator = replayFileValidator ?? new ReplayFileValidator();
    }

    public async Task<UploadOutcome> UploadReplayAsync(string filePath, string fileName, CancellationToken cancellationToken = default)
    {
        FileInfo fileInfo;
        try
        {
            fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists)
            {
                return new UploadOutcome.Failed("Could not read file: file does not exist.");
            }
        }
        catch (Exception error)
        {
            return new UploadOutcome.Failed($"Could not read file: {error.Message}");
        }

        var validation = await replayFileValidator.ValidateAsync(filePath, cancellationToken);
        if (!validation.IsValid)
        {
            return new UploadOutcome.Failed(validation.Error ?? "Invalid replay file.");
        }

        var token = await accessTokenSource.GetAccessTokenAsync();
        if (string.IsNullOrWhiteSpace(token))
        {
            return new UploadOutcome.Failed("Not signed in (no OAuth access token)");
        }

        try
        {
            var upload = await backendApiClient.RequestUploadUrlAsync(fileName, fileInfo.Length, token, cancellationToken);
            await using var stream = File.OpenRead(filePath);
            using var request = new HttpRequestMessage(new HttpMethod(upload.Method), upload.UploadUrl)
            {
                Content = new StreamContent(stream)
            };
            request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

            foreach (var header in upload.Headers)
            {
                if (!request.Headers.TryAddWithoutValidation(header.Key, header.Value))
                {
                    request.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            using var response = await uploadClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return new UploadOutcome.Uploaded();
            }

            if ((int)response.StatusCode == 403)
            {
                return new UploadOutcome.AlreadyUploaded(403);
            }

            return new UploadOutcome.Failed(
                $"Upload responded with {(int)response.StatusCode} {response.ReasonPhrase}",
                (int)response.StatusCode);
        }
        catch (BackendError error) when (error.Status == 403)
        {
            return new UploadOutcome.AlreadyUploaded(403);
        }
        catch (BackendError error)
        {
            return new UploadOutcome.Failed(error.Message, error.Status);
        }
        catch (Exception error)
        {
            return new UploadOutcome.Failed(error.Message);
        }
    }
}
