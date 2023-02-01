using Lombiq.OrchardCoreApiClient.Exceptions;
using Lombiq.OrchardCoreApiClient.Interfaces;
using Lombiq.OrchardCoreApiClient.Models;
using RestEase;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Lombiq.OrchardCoreApiClient;

[SuppressMessage(
        "Security",
        "SCS0004: Certificate Validation has been disabled.",
        Justification = "It is only disabled for local testing")]
[SuppressMessage(
        "Security",
        "S4830: Enable server certificate validation on this SSL/TLS connection",
        Justification = "It is only disabled for local testing")]
internal class ConfigurableCertificateValidatingHttpClientHandler : HttpClientHandler
{
    private readonly ApiClientSettings _apiClientSettings;

    private DateTime _expirationDateUtc = DateTime.MinValue;
    private Token _tokenResponse;

    public ConfigurableCertificateValidatingHttpClientHandler(ApiClientSettings apiClientSettings)
    {
        _apiClientSettings = apiClientSettings;

        if (_apiClientSettings.DisableCertificateValidation)
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
        }
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Headers.Authorization == null)
        {
            throw new ApiClientException($"The {nameof(request)} header should contain an authorization token.");
        }

        if (_expirationDateUtc < DateTime.UtcNow.AddSeconds(60))
        {
            var handler = new HttpClientHandler();

            if (_apiClientSettings.DisableCertificateValidation)
            {
                handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
            }

#pragma warning disable CA5399 // HttpClient is created without enabling CheckCertificateRevocationList
            var httpClient = new HttpClient(handler)
            {
                BaseAddress = _apiClientSettings.DefaultTenantUri,
            };
#pragma warning restore CA5399

            var tokenResponse = await RestClient
                .For<IOrchardCoreAuthorizationApi>(httpClient)
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

            handler.Dispose();
            httpClient.Dispose();
        }

        request.Headers.Authorization = new AuthenticationHeaderValue(
            request.Headers.Authorization.Scheme, _tokenResponse.AccessToken);

        return await base.SendAsync(request, cancellationToken);
    }
}
