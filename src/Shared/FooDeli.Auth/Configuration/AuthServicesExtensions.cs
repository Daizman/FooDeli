using FooDeli.Auth.Configuration.Options;
using FooDeli.Auth.S2S;
using FooDeli.Auth.Token;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace FooDeli.Auth.Configuration;

public static class AuthServicesExtensions
{
    public static IServiceCollection AddFooDeliAuth(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddFooDeliJwtAuthentication(configuration);
        services.AddFooDeliS2SClient(configuration);

        return services;
    }

    public static IServiceCollection AddFooDeliJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddOptionsWithValidateOnStart<KeycloakOptions>(KeycloakOptions.SectionName);

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                var keycloakOptions = configuration
                    .GetRequiredSection(KeycloakOptions.SectionName)
                    .Get<KeycloakOptions>()!;

                options.Authority = keycloakOptions.Authority;
                options.Audience = keycloakOptions.Audience;
                options.RequireHttpsMetadata = keycloakOptions.RequireHttpsMetadata;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = keycloakOptions.ValidIssuer,
                    ValidateAudience = true,
                    ValidAudience = keycloakOptions.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    NameClaimType = "preferred_username",
                    RoleClaimType = "roles"
                };
            });

        services.AddAuthorization();

        return services;
    }

    public static IServiceCollection AddFooDeliS2SClient(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddOptionsWithValidateOnStart<S2SClientOptions>(S2SClientOptions.SectionName);

        services.AddHttpClient<ITokenClient, KeycloakTokenClient>();

        services.AddTransient<TokenDelegatingHandler>();

        return services;
    }

    public static IHttpClientBuilder AddS2SHttpClient<TClient, TImplementation>(
        this IServiceCollection services,
        Action<HttpClient>? configureClient = null
    )
        where TClient : class
        where TImplementation : class, TClient
    {
        var builder = services.AddHttpClient<TClient, TImplementation>();

        if (configureClient is not null)
        {
            builder.ConfigureHttpClient(configureClient);
        }

        builder.AddHttpMessageHandler<TokenDelegatingHandler>();

        return builder;
    }

    public static IHttpClientBuilder AddS2SHttpClient(
        this IServiceCollection services,
        string name,
        Action<HttpClient>? configureClient = null
    )
    {
        var builder = services.AddHttpClient(name);

        if (configureClient is not null)
        {
            builder.ConfigureHttpClient(configureClient);
        }

        builder.AddHttpMessageHandler<TokenDelegatingHandler>();

        return builder;
    }
}

