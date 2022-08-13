using Lombiq.OrchardCoreApiClient.Constants;
using Lombiq.OrchardCoreApiClient.Exceptions;
using Lombiq.OrchardCoreApiClient.Interfaces;
using Lombiq.OrchardCoreApiClient.Models;
using RestEase;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Lombiq.OrchardCoreApiClient;

public class ApiClient
{
    private readonly ApiClientSettings _apiClientSettings;
    private Lazy<IOrchardCoreApi> LazyOrchardCoreApi => new(() => GetOrchardCoreApi(_apiClientSettings.DefaultTenantUri));
    public IOrchardCoreApi OrchardCoreApi => LazyOrchardCoreApi.Value;
    private DateTime ExpirationDate = DateTime.MinValue;
    private Token TokenResponse;

    public ApiClient(ApiClientSettings apiClientSettings) => _apiClientSettings = apiClientSettings;

    private IOrchardCoreApi GetOrchardCoreApi(Uri defaultTenantUri) =>
        RestClient.For<IOrchardCoreApi>(defaultTenantUri, async (request, _) =>
        {
            if (request.Headers.Authorization != null)
            {
                if (ExpirationDate < DateTime.UtcNow.AddSeconds(300))
                {
                    var tokenResponse = await RestClient.
                        For<IOrchardCoreAuthorizatonApi>(defaultTenantUri).TokenAsync(
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

                    int tokenExpiration = int.Parse(tokenResponse.GetContent().ExpiresIn, CultureInfo.CurrentCulture);

                    ExpirationDate = DateTime.UtcNow.AddSeconds(tokenExpiration);

                    TokenResponse = tokenResponse.GetContent();
                }

                request.Headers.Authorization = new AuthenticationHeaderValue(
                    request.Headers.Authorization.Scheme, TokenResponse.AccessToken);
            }
        });

    public async Task CreateAndSetupTenantAsync(
        CreateApiViewModel createApiViewModel,
        SetupApiViewModel setupApiViewModel)
    {
        if (!Timezones.GetTimezoneIds().Contains(setupApiViewModel.SiteTimeZone))
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
