using Lombiq.OrchardCoreApiClient.Tester.Models;
using RestEase;
using System.Threading.Tasks;

namespace Lombiq.OrchardCoreApiClient.Tester.Interfaces;

/// <summary>
/// Interface representing the subset of APIs of Orchard Core.
/// </summary>
public interface IOrchardCoreApiTester
{
    /// <summary>
    /// Create a new tenant in Orchard Core.
    /// </summary>
    /// <param name="createTenantParameters">
    /// The neccessary parameters to create a tenant: Name, RequestUrlPrefix etc.
    /// </param>
    /// <returns>Returns the response of the tenant creation.</returns>
    [Post("api/tenants/create")]
    [Header("Authorization", "Bearer")]
    Task<Response<string>> CreateAsync([Body] CreateApiViewModel createTenantParameters);

    /// <summary>
    /// Setup the previously created tenant in Orchard Core.
    /// </summary>
    /// <param name="setupTenantParameters">
    /// The neccessary parameters to setup a tenant: Name, ConnectionString etc.
    /// </param>
    /// <returns>Returns the response of the tenant setup.</returns>
    [Post("api/tenants/setup")]
    [Header("Authorization", "Bearer")]
    Task<Response<string>> SetupAsync([Body] SetupApiViewModel setupTenantParameters);
}
