using Lombiq.HelpfulLibraries.Refit.Helpers;
using Lombiq.OrchardCoreApiClient.Models;
using System;
using System.Net.Http;

namespace Lombiq.OrchardCoreApiClient;

public class ApiClientBase<T> : IDisposable
{
    private readonly Lazy<T> _lazyOrchardCoreApi;

    private HttpClient _httpClient;

    public T OrchardCoreApi => _lazyOrchardCoreApi.Value;

    protected ApiClientBase(ApiClientSettings apiClientSettings) =>
        _lazyOrchardCoreApi = new(() =>
        {
            _httpClient = ConfigurableCertificateValidatingHttpClientHandler.CreateClient(apiClientSettings);

            // We use Newtonsoft Json.NET because Orchard Core uses it too, so the models will behave the same.
            return RefitHelper.WithNewtonsoftJson<T>(_httpClient);
        });

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
