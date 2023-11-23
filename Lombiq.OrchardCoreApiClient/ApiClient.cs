using Lombiq.OrchardCoreApiClient.Constants;
using Lombiq.OrchardCoreApiClient.Exceptions;
using Lombiq.OrchardCoreApiClient.Interfaces;
using Lombiq.OrchardCoreApiClient.Models;
using Refit;
using System.Threading.Tasks;

namespace Lombiq.OrchardCoreApiClient;

public class ApiClient : ApiClientBase<IOrchardCoreApi>
{
    public ApiClient(ApiClientSettings apiClientSettings)
        : base(apiClientSettings)
    {
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
}
