using System.ComponentModel.DataAnnotations;

namespace FooDeli.Auth.Configuration.Options;

public sealed record KeycloakOptions(
    [property: Required] string Authority,
    [property: Required] string Realm,
    [property: Required] string Audience,
    bool RequireHttpsMetadata = true
)
{
    public const string SectionName = "Keycloak";

    public string MetadataAddress => $"{Authority}/realms/{Realm}/.well-known/openid-configuration";

    public string ValidIssuer => $"{Authority}/realms/{Realm}";
}
