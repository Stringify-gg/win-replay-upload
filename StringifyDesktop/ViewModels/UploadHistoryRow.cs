using StringifyDesktop.Models;

namespace StringifyDesktop.ViewModels;

public sealed record UploadHistoryRow(
    string FileName,
    UploadStatus Status,
    DateTimeOffset At,
    string? Detail)
{
    public string StatusText => Status switch
    {
        UploadStatus.Uploaded => "uploaded",
        UploadStatus.AlreadyUploaded => "already-uploaded",
        UploadStatus.Failed => "failed",
        _ => Status.ToString()
    };

    public string WhenText => At.LocalDateTime.ToString("g");
}
