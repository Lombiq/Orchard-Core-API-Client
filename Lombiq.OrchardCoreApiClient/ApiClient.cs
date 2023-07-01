using Lombiq.HelpfulLibraries.Refit.Helpers;
using Lombiq.OrchardCoreApiClient.Constants;
using Lombiq.OrchardCoreApiClient.Exceptions;
using Lombiq.OrchardCoreApiClient.Interfaces;
using Lombiq.OrchardCoreApiClient.Models;
using Refit;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Lombiq.OrchardCoreApiClient;

public class ApiClient : IDisposable
{
    private readonly Lazy<IOrchardCoreApi> _lazyOrchardCoreApi;

    private HttpClient _httpClient;

    public IOrchardCoreApi OrchardCoreApi => _lazyOrchardCoreApi.Value;

    public ApiClient(ApiClientSettings apiClientSettings) =>
        _lazyOrchardCoreApi = new(() =>
        {
            _httpClient = ConfigurableCertificateValidatingHttpClientHandler.CreateClient(apiClientSettings);

            // We use Newtonsoft Json.NET because Orchard Core uses it too, so the models will behave the same.
            return RefitHelper.WithNewtonsoftJson<IOrchardCoreApi>(_httpClient);
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
            using var response = await OrchardCoreApi.CreateAsync(createApiViewModel).ConfigureAwait(false);
            await response.EnsureSuccessStatusCodeAsync();
        }
        catch (ApiException ex)
        {
            throw new ApiClientException("Tenant creation failed.", ex);
        }

        try
        {
            await OrchardCoreApi.SetupAsync(setupApiViewModel).ConfigureAwait(false);
        }
        catch (ApiException ex)
        {
            throw new ApiClientException("Tenant setup failed.", ex);
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_lazyOrchardCoreApi.IsValueCreated && _httpClient != null)
        {
            _httpClient.Dispose();
            _httpClient = null;
        }
    }
}
