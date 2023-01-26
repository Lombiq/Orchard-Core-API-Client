using Lombiq.OrchardCoreApiClient.Models;
using RestEase;
using System.Threading.Tasks;

namespace Lombiq.OrchardCoreApiClient.Interfaces;

/// <summary>
/// Interface representing the subset of APIs of Orchard Core.
/// </summary>
public interface IOrchardCoreApi
{
    /// <summary>
    /// Create a new tenant in Orchard Core.
    /// </summary>
    /// <param name="createTenantParameters">
    /// The necessary parameters to create a tenant: Name, RequestUrlPrefix etc.
    /// </param>
    /// <returns>The response of the tenant creation.</returns>
    [Post("api/tenants/create")]
    [Header("Authorization", "Bearer")]
    Task<Response<string>> CreateAsync([Body] CreateApiViewModel createTenantParameters);

    /// <summary>
    /// Setup the previously created tenant in Orchard Core.
    /// </summary>
    /// <param name="setupTenantParameters">
    /// The necessary parameters to set up a tenant: Name, ConnectionString etc.
    /// </param>
    /// <returns>The response of the tenant setup.</returns>
    [Post("api/tenants/setup")]
    [Header("Authorization", "Bearer")]
    Task<Response<string>> SetupAsync([Body] SetupApiViewModel setupTenantParameters);

    /// <summary>
    /// Edit a previously created tenant in Orchard Core.
    /// </summary>
    /// <param name="editTenantParameters">The necessary parameter to edit a tenant: Name.</param>
    /// <returns>The response of the tenant edit.</returns>
    [Post("api/tenants/edit")]
    [Header("Authorization", "Bearer")]
    Task<Response<string>> EditAsync([Body] EditApiViewModel editTenantParameters);
}
