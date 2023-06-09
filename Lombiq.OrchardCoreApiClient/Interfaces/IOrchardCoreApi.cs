using Lombiq.OrchardCoreApiClient.Models;
using Refit;
using System.Threading.Tasks;
using static Lombiq.OrchardCoreApiClient.Constants.CommonHeaders;

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
    [Headers(AuthorizationBearer)]
    Task<string> CreateAsync([Body] TenantApiModel createTenantParameters);

    /// <summary>
    /// Setup the previously created tenant in Orchard Core.
    /// </summary>
    /// <param name="setupTenantParameters">
    /// The necessary parameters to set up a tenant: Name, ConnectionString etc.
    /// </param>
    /// <returns>The response of the tenant setup.</returns>
    [Post("api/tenants/setup")]
    [Headers(AuthorizationBearer)]
    Task<string> SetupAsync([Body] TenantSetupApiModel setupTenantParameters);

    /// <summary>
    /// Edit a previously created tenant in Orchard Core.
    /// </summary>
    /// <param name="editTenantParameters">The necessary parameter to edit a tenant: Name.</param>
    /// <returns>The response of the tenant edit.</returns>
    [Post("api/tenants/edit")]
    [Headers(AuthorizationBearer)]
    Task<string> EditAsync([Body] TenantApiModel editTenantParameters);

    /// <summary>
    /// Edit a previously created tenant in Orchard Core with additional settings inside
    /// <see cref="TenantApiModel.CustomSettings"/>. This endpoint is not implemented, implement on your own.
    /// </summary>
    /// <param name="editTenantParameters">The <see cref="TenantApiModel.CustomSettings"/> property in the
    /// <see cref="TenantApiModel"/> is the additional property that is not processed in <see cref="EditAsync"/>.</param>
    /// <returns>The response of the custom tenant edit.</returns>
    [Post("api/tenants/custom-edit")]
    [Headers(AuthorizationBearer)]
    Task<string> CustomEditAsync([Body] TenantApiModel editTenantParameters);

    /// <summary>
    /// Remove a previously created tenant in Orchard Core.
    /// </summary>
    /// <param name="tenantName">The necessary parameter to remove a tenant.</param>
    /// <returns>The response of the tenant removal.</returns>
    [Post("api/tenants/remove/{tenantName}")]
    [Headers(AuthorizationBearer)]
    Task<string> RemoveAsync(string tenantName);

    /// <summary>
    /// Disable a previously created tenant in Orchard Core.
    /// </summary>
    /// <param name="tenantName">The necessary parameter to disable a tenant.</param>
    /// <returns>The response of the tenant disable.</returns>
    [Post("api/tenants/disable/{tenantName}")]
    [Headers(AuthorizationBearer)]
    Task<string> DisableAsync(string tenantName);

    /// <summary>
    /// Enable a previously disabled tenant in Orchard Core.
    /// </summary>
    /// <param name="tenantName">The necessary parameter to enable a tenant.</param>
    /// <returns>The response of the tenant enable.</returns>
    [Post("api/tenants/enable/{tenantName}")]
    [Headers(AuthorizationBearer)]
    Task<string> EnableAsync(string tenantName);
}
