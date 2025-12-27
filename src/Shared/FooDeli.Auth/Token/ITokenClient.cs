namespace FooDeli.Auth.Token;

public interface ITokenClient
{
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}
