using Lombiq.OrchardCoreApiClient.Interfaces;
using Lombiq.OrchardCoreApiClient.Models;

namespace Lombiq.OrchardCoreApiClient;

public class OrchardCoreApiClient : ApiClient<IOrchardCoreApi>
{
    public OrchardCoreApiClient(ApiClientSettings apiClientSettings)
        : base(apiClientSettings)
    {
    }
}
