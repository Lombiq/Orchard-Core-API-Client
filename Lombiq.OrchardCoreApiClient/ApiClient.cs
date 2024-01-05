using Lombiq.HelpfulLibraries.Refit.Helpers;
using Lombiq.OrchardCoreApiClient.Constants;
using Lombiq.OrchardCoreApiClient.Exceptions;
using Lombiq.OrchardCoreApiClient.Interfaces;
using Lombiq.OrchardCoreApiClient.Models;
using Refit;
using Polly;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Polly.Retry;

namespace Lombiq.OrchardCoreApiClient;

public class ApiClient : ApiClient<IOrchardCoreApi>
{
    public ApiClient(ApiClientSettings apiClientSettings)
        : base(apiClientSettings)
    {
    }
}

public class ApiClient<TApi> : IDisposable
    where TApi : IOrchardCoreApi
{
    private readonly Lazy<TApi> _lazyOrchardCoreApi;

    private HttpClient _httpClient;

    public AsyncRetryPolicy RetryPolicy { get; set; }

    public TApi OrchardCoreApi => _lazyOrchardCoreApi.Value;

    public ApiClient(ApiClientSettings apiClientSettings)
    {
        _lazyOrchardCoreApi = new(() =>
        {
            _httpClient = ConfigurableCertificateValidatingHttpClientHandler.CreateClient(apiClientSettings);

            // We use Newtonsoft Json.NET because Orchard Core uses it too, so the models will behave the same.
            return RefitHelper.WithNewtonsoftJson<TApi>(_httpClient);
        });

        RetryPolicy = InitRetryPolicy();
    }

    public AsyncRetryPolicy InitRetryPolicy(
        int retryCount = 3,
        Func<int,TimeSpan> sleepDurationProvider = null,
        Func<Exception, TimeSpan, int, Context, Task> onRetryAsync = null) =>
        Policy
            .Handle<ApiException>()
            .Or<ApiClientException>()
            .Or<HttpRequestException>()
            .WaitAndRetryAsync(
                retryCount,
                sleepDurationProvider ?? (_ => TimeSpan.FromSeconds(2)),
                onRetryAsync ?? ((_, _, _, _) => Task.CompletedTask));

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
