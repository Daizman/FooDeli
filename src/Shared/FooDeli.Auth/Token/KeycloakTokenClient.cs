using System.Net.Http.Json;
using FooDeli.Auth.Configuration.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FooDeli.Auth.Token;

public sealed class KeycloakTokenClient : ITokenClient, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly S2SClientOptions _options;
    private readonly ILogger<KeycloakTokenClient> _logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private string? _cachedToken;
    private DateTimeOffset _tokenExpiry = DateTimeOffset.MinValue;

    public KeycloakTokenClient(
        HttpClient httpClient,
        IOptions<S2SClientOptions> options,
        ILogger<KeycloakTokenClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        // Быстрая проверка без блокировки
        if (_cachedToken is not null && !IsTokenExpiringSoon())
        {
            return _cachedToken;
        }

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            if (_cachedToken is not null && !IsTokenExpiringSoon())
            {
                return _cachedToken;
            }

            return await RefreshTokenAsync(cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private bool IsTokenExpiringSoon()
    {
        var threshold = TimeSpan.FromSeconds(_options.TokenRefreshThresholdSeconds);
        return DateTimeOffset.UtcNow >= _tokenExpiry - threshold;
    }

    private async Task<string> RefreshTokenAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Requesting new access token from Keycloak");

        var requestBody = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret
        };

        if (!string.IsNullOrEmpty(_options.Scope))
        {
            requestBody["scope"] = _options.Scope;
        }

        var request = new HttpRequestMessage(HttpMethod.Post, _options.TokenEndpoint)
        {
            Content = new FormUrlEncodedContent(requestBody)
        };

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "Failed to obtain access token from Keycloak. Status: {StatusCode}, Response: {Response}",
                response.StatusCode,
                errorContent);

            throw new HttpRequestException(
                $"Failed to obtain access token from Keycloak. Status: {response.StatusCode}");
        }

        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Failed to deserialize token response");

        _cachedToken = tokenResponse.AccessToken;
        _tokenExpiry = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn);

        _logger.LogDebug(
            "Successfully obtained access token, expires at {Expiry}",
            _tokenExpiry);

        return _cachedToken;
    }

    public void Dispose()
    {
        _semaphore.Dispose();
    }
}
