using System.ComponentModel.DataAnnotations;

namespace FooDeli.Auth.Configuration.Options;

public sealed record S2SClientOptions(
    [property: Required] string TokenEndpoint,
    [property: Required] string ClientId,
    [property: Required] string ClientSecret,
    string? Scope = null,
    int TokenRefreshThresholdSeconds = 30
)
{
    public const string SectionName = "S2SClient";
}
