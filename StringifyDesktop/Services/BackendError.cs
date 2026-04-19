namespace StringifyDesktop.Services;

public sealed class BackendError : Exception
{
    public BackendError(string message, int status)
        : base(message)
    {
        Status = status;
    }

    public int Status { get; }
}
