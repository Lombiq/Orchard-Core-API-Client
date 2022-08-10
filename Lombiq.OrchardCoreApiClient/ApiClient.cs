using Lombiq.OrchardCoreApiClient.Exceptions;
using Lombiq.OrchardCoreApiClient.Interfaces;
using Lombiq.OrchardCoreApiClient.Models;
using RestEase;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;

namespace Lombiq.OrchardCoreApiClient;

public class ApiClient
{
    private readonly ApiClientSettings _apiClientSettings;
    private readonly IOrchardCoreApi _orchardCoreApi;

    public ApiClient(ApiClientSettings apiClientSettings)
    {
        _apiClientSettings = apiClientSettings;
        _orchardCoreApi = GetOrchardCoreApi(_apiClientSettings.DefaultTenantUri);
    }

    private IOrchardCoreApi GetOrchardCoreApi(Uri defaultTenantUri) =>
        RestClient.For<IOrchardCoreApi>(defaultTenantUri, async (request, _) =>
        {
            if (request.Headers.Authorization != null)
            {
                var tokenResponse = await RestClient.
                For<IOrchardCoreAuthorizatonApi>(defaultTenantUri).
                TokenAsync(
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

                if (tokenResponse.GetContent().ExpiresIn == "0")
                {
                    throw new ApiClientException($"The token is expired.");
                }

                request.Headers.Authorization = new AuthenticationHeaderValue(
                    request.Headers.Authorization.Scheme, tokenResponse.GetContent().AccessToken);
            }
        });
}
