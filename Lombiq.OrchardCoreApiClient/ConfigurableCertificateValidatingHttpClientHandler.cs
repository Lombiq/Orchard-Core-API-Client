using Lombiq.HelpfulLibraries.Refit.Helpers;
using Lombiq.OrchardCoreApiClient.Exceptions;
using Lombiq.OrchardCoreApiClient.Interfaces;
using Lombiq.OrchardCoreApiClient.Models;
using Refit;
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
        Justification = "It's only disabled optionally, like for local testing.")]
[SuppressMessage(
        "Security",
        "S4830: Enable server certificate validation on this SSL/TLS connection",
        Justification = "It's only disabled optionally, like for local testing.")]
internal sealed class ConfigurableCertificateValidatingHttpClientHandler : HttpClientHandler
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
            using var handler = new HttpClientHandler();

            if (_apiClientSettings.DisableCertificateValidation)
            {
                handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
                handler.CheckCertificateRevocationList = true;
            }

            // It's only disabled optionally, like for local testing.
#pragma warning disable CA5400 // Ensure HttpClient certificate revocation list check is not disabled
            using var httpClient = new HttpClient(handler)
            {
                BaseAddress = _apiClientSettings.DefaultTenantUri,
            };
#pragma warning restore CA5400

            var tokenResponse = await RefitHelper
                .WithNewtonsoft<IOrchardCoreAuthorizationApi>(httpClient)
                .TokenAsync(
                    new Dictionary<string, string>
                    {
                        ["grant_type"] = "client_credentials",
                        ["client_id"] = _apiClientSettings.ClientId,
                        ["client_secret"] = _apiClientSettings.ClientSecret,
                    });

            if (tokenResponse.Error is { } error)
            {
                throw new ApiClientException(
                    $"API client setup failed. An error occurred while retrieving an access token: {error}");
            }

            int tokenExpiration = int.Parse(tokenResponse.ExpiresIn, CultureInfo.InvariantCulture);
            _expirationDateUtc = DateTime.UtcNow.AddSeconds(tokenExpiration);

            _tokenResponse = tokenResponse;
        }

        request.Headers.Authorization = new AuthenticationHeaderValue(
            request.Headers.Authorization.Scheme, _tokenResponse.AccessToken);

        return await base.SendAsync(request, cancellationToken);
    }
}
