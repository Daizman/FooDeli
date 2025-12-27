using System.Net.Http.Headers;
using FooDeli.Auth.Token;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace FooDeli.Auth.S2S;

public sealed class TokenDelegatingHandler(ITokenClient tokenService) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        var token = await tokenService.GetAccessTokenAsync(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, token);

        return await base.SendAsync(request, cancellationToken);
    }
}
