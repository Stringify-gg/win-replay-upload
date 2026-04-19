namespace StringifyDesktop.Models;

public sealed record ReplayValidationResult(bool IsValid, string? Error = null)
{
    public static ReplayValidationResult Valid { get; } = new(true);
}
