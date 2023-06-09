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

    private ConfigurableCertificateValidatingHttpClientHandler _certificateValidatingHandler;
    private HttpClient _httpClient;

    public IOrchardCoreApi OrchardCoreApi => _lazyOrchardCoreApi.Value;

    public ApiClient(ApiClientSettings apiClientSettings) =>
        _lazyOrchardCoreApi = new(() =>
        {
            _certificateValidatingHandler = new ConfigurableCertificateValidatingHttpClientHandler(apiClientSettings);

            _httpClient = new HttpClient(_certificateValidatingHandler)
            {
                BaseAddress = apiClientSettings.DefaultTenantUri,
            };

            return RestService.For<IOrchardCoreApi>(_httpClient);
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

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_lazyOrchardCoreApi.IsValueCreated) return;

        object api = _lazyOrchardCoreApi.Value;
        (api as IDisposable)?.Dispose();

        if (_httpClient != null)
        {
            _httpClient.Dispose();
            _httpClient = null;
        }

        if (_certificateValidatingHandler != null)
        {
            _certificateValidatingHandler.Dispose();
            _certificateValidatingHandler = null;
        }
    }
}
