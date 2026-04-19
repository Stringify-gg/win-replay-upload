namespace StringifyDesktop.Models;

public abstract record UploadOutcome
{
    private UploadOutcome() { }

    public sealed record Uploaded : UploadOutcome;

    public sealed record AlreadyUploaded(int HttpStatus) : UploadOutcome;

    public sealed record Failed(string Error, int? HttpStatus = null) : UploadOutcome;
}
