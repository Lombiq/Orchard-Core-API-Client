using Lombiq.OrchardCoreApiClient.Constants;
using Lombiq.OrchardCoreApiClient.Exceptions;
using Lombiq.OrchardCoreApiClient.Interfaces;
using Lombiq.OrchardCoreApiClient.Models;
using RestEase;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Lombiq.OrchardCoreApiClient;

public class ApiClient
{
    private Lazy<IOrchardCoreApi> LazyOrchardCoreApi => new(() => RestClient.For<IOrchardCoreApi>(_httpClient));
    protected readonly ApiClientSettings _apiClientSettings;
    protected readonly HttpClient _httpClient;

    public IOrchardCoreApi OrchardCoreApi => LazyOrchardCoreApi.Value;

    public ApiClient(ApiClientSettings apiClientSettings)
    {
        _apiClientSettings = apiClientSettings;

        // This needs to use the factory.
        _httpClient = new HttpClient(new ConfigurablyCertificateValidatingHttpClientHandler(_apiClientSettings))
        {
            BaseAddress = _apiClientSettings.DefaultTenantUri,
        };
    }

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
