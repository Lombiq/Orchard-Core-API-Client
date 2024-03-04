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

    private ConfigurableCertificateValidatingHttpClientHandler(ApiClientSettings apiClientSettings)
    {
        _apiClientSettings = apiClientSettings;
        ApplyCertificationValidationSetting(this, _apiClientSettings);
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Headers.Authorization == null)
        {
            throw new ApiClientException($"The {nameof(request)} header should contain an authorization token.");
        }

        if (_expirationDateUtc < DateTime.UtcNow.AddSeconds(60))
        {
            using var httpClient = CreateClient(createBasicHandler: true, _apiClientSettings);

            Token tokenResponse;
            try
            {
                tokenResponse = await RestService
                    .For<IOrchardCoreAuthorizationApi>(httpClient)
                    .TokenAsync(
                        new Dictionary<string, string>
                        {
                            ["grant_type"] = "client_credentials",
                            ["client_id"] = _apiClientSettings.ClientId,
                            ["client_secret"] = _apiClientSettings.ClientSecret,
                        });
            }
            catch (Exception exception)
            {
                throw new ApiClientException(
                    $"API client setup failed. An error occurred while retrieving an access token: {exception.Message}",
                    exception);
            }

            if (tokenResponse.Error is { } error)
            {
                throw new ApiClientException(
                    $"API client setup failed. An error occurred while retrieving an access token: {error}");
            }

            _expirationDateUtc = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);

            _tokenResponse = tokenResponse;
        }

        request.Headers.Authorization = new AuthenticationHeaderValue(
            _tokenResponse.TokenType,
            _tokenResponse.AccessToken);

        return await base.SendAsync(request, cancellationToken);
    }

    [SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "Disposed by the HttpClient.")]
    [SuppressMessage(
        "Security",
        "CA5400:HttpClient may be created without enabling CheckCertificateRevocationList",
        Justification = "It's only disabled optionally, like for local testing.")]
    private static HttpClient CreateClient(bool createBasicHandler, ApiClientSettings settings) =>
        new(ApplyCertificationValidationSetting(
            createBasicHandler
                ? new HttpClientHandler()
                : new ConfigurableCertificateValidatingHttpClientHandler(settings),
            settings))
        {
            BaseAddress = settings.DefaultTenantUri,
        };

    public static HttpClient CreateClient(ApiClientSettings settings) =>
        CreateClient(createBasicHandler: false, settings);

    public static HttpClientHandler ApplyCertificationValidationSetting(
        HttpClientHandler handler,
        ApiClientSettings settings)
    {
        if (settings.DisableCertificateValidation)
        {
            handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
            handler.CheckCertificateRevocationList = true;
        }

        return handler;
    }
}
