using Lombiq.HelpfulLibraries.Refit.Helpers;
using Lombiq.OrchardCoreApiClient.Exceptions;
using Lombiq.OrchardCoreApiClient.Interfaces;
using Lombiq.OrchardCoreApiClient.Models;
using Polly;
using Polly.Retry;
using Refit;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Lombiq.OrchardCoreApiClient;

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
        Func<int, TimeSpan> sleepDurationProvider = null,
        Func<Exception, TimeSpan, int, Context, Task> onRetryAsync = null) =>
        Policy
            .Handle<ApiException>()
            .Or<ApiClientException>()
            .Or<HttpRequestException>()
            .WaitAndRetryAsync(
                retryCount,
                sleepDurationProvider ?? (_ => TimeSpan.FromSeconds(2)),
                onRetryAsync ?? ((_, _, _, _) => Task.CompletedTask));

    public async Task<TResult> ExecuteWithRetryPolicyAsync<TResult>(
        Func<Task<TResult>> executeAction,
        Func<ApiClientException, Task> catchAction)
    {
        try
        {
            return await RetryPolicy.ExecuteAsync(executeAction);
        }
        catch (ApiClientException ex)
        {
            await catchAction(ex);

            // Throw the exception again so the caller can handle it as well.
            throw;
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
