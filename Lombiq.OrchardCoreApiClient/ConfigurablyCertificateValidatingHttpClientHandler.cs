using Lombiq.OrchardCoreApiClient.Exceptions;
using Lombiq.OrchardCoreApiClient.Interfaces;
using Lombiq.OrchardCoreApiClient.Models;
using RestEase;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Lombiq.OrchardCoreApiClient;

internal class ConfigurablyCertificateValidatingHttpClientHandler : HttpClientHandler
{
    private readonly ApiClientSettings _apiClientSettings;

    private DateTime _expirationDateUtc = DateTime.MinValue;
    private Token _tokenResponse;

    public ConfigurablyCertificateValidatingHttpClientHandler(ApiClientSettings apiClientSettings)
    {
        _apiClientSettings = apiClientSettings;

        // Conditionally disable certificate validation, based on ApiClientSettings.
        ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Headers.Authorization == null)
        {
            throw new ApiClientException("The request header should contain an authorization token.");
        }

        if (_expirationDateUtc < DateTime.UtcNow.AddSeconds(60))
        {
            var tokenResponse = await RestClient
                .For<IOrchardCoreAuthorizationApi>(_apiClientSettings.DefaultTenantUri)
                .TokenAsync(
                    new Dictionary<string, string>
                    {
                        ["grant_type"] = "client_credentials",
                        ["client_id"] = _apiClientSettings.ClientId,
                        ["client_secret"] = _apiClientSettings.ClientSecret,
                    });

            tokenResponse.ResponseMessage.EnsureSuccessStatusCode();

            if (tokenResponse.GetContent().Error != null)
            {
                throw new ApiClientException(
                    $"API client setup failed. An error occurred while retrieving an access token: " +
                    tokenResponse.GetContent().Error);
            }

            int tokenExpiration = int.Parse(tokenResponse.GetContent().ExpiresIn, CultureInfo.InvariantCulture);

            _expirationDateUtc = DateTime.UtcNow.AddSeconds(tokenExpiration);

            _tokenResponse = tokenResponse.GetContent();
        }

        request.Headers.Authorization = new AuthenticationHeaderValue(
            request.Headers.Authorization.Scheme, _tokenResponse.AccessToken);

        return await base.SendAsync(request, cancellationToken);
    }
}
