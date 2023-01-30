using Lombiq.OrchardCoreApiClient.Constants;
using Lombiq.OrchardCoreApiClient.Exceptions;
using Lombiq.OrchardCoreApiClient.Interfaces;
using Lombiq.OrchardCoreApiClient.Models;
using RestEase;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Lombiq.OrchardCoreApiClient;

public class ApiClient
{
    private Lazy<IOrchardCoreApi> LazyOrchardCoreApi => new(() => GetOrchardCoreApi(_apiClientSettings.DefaultTenantUri));
    protected readonly ApiClientSettings _apiClientSettings;
    protected readonly HttpClient _httpClient;
    protected DateTime _expirationDateUtc = DateTime.MinValue;
    protected Token _tokenResponse;
    public IOrchardCoreApi OrchardCoreApi => LazyOrchardCoreApi.Value;

    public ApiClient(ApiClientSettings apiClientSettings, HttpClient httpClient = null)
    {
        _apiClientSettings = apiClientSettings;
        _httpClient = httpClient ?? new HttpClient();
    }

    private IOrchardCoreApi GetOrchardCoreApi(Uri defaultTenantUri) =>
        RestClient.For<IOrchardCoreApi>(defaultTenantUri, async (request, _) =>
        {
            if (request.Headers.Authorization == null)
            {
                throw new ApiClientException("The request header should contain an authorization token.");
            }

            if (_expirationDateUtc < DateTime.UtcNow.AddSeconds(60))
            {
                var tokenResponse = await RestClient
                    .For<IOrchardCoreAuthorizationApi>(defaultTenantUri)
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
        });

    public async Task CreateAndSetupTenantAsync(
        TenantApiModel createApiViewModel,
        TenantSetupApiModel setupApiViewModel)
    {
        if (!Timezones.TimezoneIds.Contains(setupApiViewModel.SiteTimeZone))
        {
            throw new ApiClientException(
                $"Invalid timezone ID {setupApiViewModel.SiteTimeZone}. For the list of all valid timezone IDs " +
                "follow this list: https://gist.github.com/jrolstad/5ca7d78dbfe182d7c1be");
        }

        try
        {
            (await OrchardCoreApi.
                CreateAsync(createApiViewModel).
                ConfigureAwait(false)).
                ResponseMessage.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            throw new ApiClientException("Tenant creation failed.", ex);
        }

        try
        {
            (await OrchardCoreApi.
                SetupAsync(setupApiViewModel).
                ConfigureAwait(false)).
                ResponseMessage.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            throw new ApiClientException("Tenant setup failed.", ex);
        }
    }
}
