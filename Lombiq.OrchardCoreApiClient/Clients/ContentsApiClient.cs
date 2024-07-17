using Lombiq.OrchardCoreApiClient.Interfaces;
using Lombiq.OrchardCoreApiClient.Models;

namespace Lombiq.OrchardCoreApiClient.Clients;

public class ContentsApiClient : ApiClient<IOrchardCoreContentsApi>
{
    public ContentsApiClient(ApiClientSettings apiClientSettings)
        : base(apiClientSettings)
    {
    }
}
