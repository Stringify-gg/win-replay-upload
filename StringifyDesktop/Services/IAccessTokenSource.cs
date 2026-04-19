namespace StringifyDesktop.Services;

public interface IAccessTokenSource
{
    Task<string?> GetAccessTokenAsync();
}
