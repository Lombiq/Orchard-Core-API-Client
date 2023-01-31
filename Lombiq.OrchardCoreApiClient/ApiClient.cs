using Lombiq.OrchardCoreApiClient.Constants;
using Lombiq.OrchardCoreApiClient.Exceptions;
using Lombiq.OrchardCoreApiClient.Interfaces;
using Lombiq.OrchardCoreApiClient.Models;
using RestEase;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Lombiq.OrchardCoreApiClient;

public class ApiClient : IDisposable
{
    protected readonly ApiClientSettings _apiClientSettings;
    private Lazy<IOrchardCoreApi> LazyOrchardCoreApi => new(() => RestClient.For<IOrchardCoreApi>(_httpClient));
    private ConfigurableCertificateValidatingHttpClientHandler _certificateValidatingHandler;
    protected HttpClient _httpClient;

    public IOrchardCoreApi OrchardCoreApi => LazyOrchardCoreApi.Value;

    public ApiClient(ApiClientSettings apiClientSettings)
    {
        _apiClientSettings = apiClientSettings;

        _certificateValidatingHandler = new ConfigurableCertificateValidatingHttpClientHandler(_apiClientSettings);
        _httpClient = new HttpClient(_certificateValidatingHandler)
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

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
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
