using Lombiq.OrchardCoreApiClient.Models;
using OrchardCore.ContentManagement;
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
    [Post("/api/tenants/create/")]
    [Headers(AuthorizationBearer, RequestedWithXmlHttpRequest)]
    Task<ApiResponse<string>> CreateAsync([Body(buffered: true)] TenantApiModel createTenantParameters);

    /// <summary>
    /// Setup the previously created tenant in Orchard Core.
    /// </summary>
    /// <param name="setupTenantParameters">
    /// The necessary parameters to set up a tenant: Name, ConnectionString etc.
    /// </param>
    /// <returns>The response of the tenant setup.</returns>
    [Post("/api/tenants/setup")]
    [Headers(AuthorizationBearer, RequestedWithXmlHttpRequest)]
    Task<ApiResponse<string>> SetupAsync([Body(buffered: true)] TenantSetupApiModel setupTenantParameters);

    /// <summary>
    /// Edit a previously created tenant in Orchard Core.
    /// </summary>
    /// <param name="editTenantParameters">The necessary parameter to edit a tenant: Name.</param>
    /// <returns>The response of the tenant edit.</returns>
    [Post("/api/tenants/edit")]
    [Headers(AuthorizationBearer, RequestedWithXmlHttpRequest)]
    Task<ApiResponse<string>> EditAsync([Body(buffered: true)] TenantApiModel editTenantParameters);

    /// <summary>
    /// Remove a previously created tenant in Orchard Core.
    /// </summary>
    /// <param name="tenantName">The necessary parameter to remove a tenant.</param>
    /// <returns>The response of the tenant removal.</returns>
    [Post("/api/tenants/remove/{tenantName}")]
    [Headers(AuthorizationBearer, RequestedWithXmlHttpRequest)]
    Task<ApiResponse<string>> RemoveAsync(string tenantName);

    /// <summary>
    /// Disable a previously created tenant in Orchard Core.
    /// </summary>
    /// <param name="tenantName">The necessary parameter to disable a tenant.</param>
    /// <returns>The response of the tenant disable.</returns>
    [Post("/api/tenants/disable/{tenantName}")]
    [Headers(AuthorizationBearer, RequestedWithXmlHttpRequest)]
    Task<ApiResponse<string>> DisableAsync(string tenantName);

    /// <summary>
    /// Enable a previously disabled tenant in Orchard Core.
    /// </summary>
    /// <param name="tenantName">The necessary parameter to enable a tenant.</param>
    /// <returns>The response of the tenant enable.</returns>
    [Post("/api/tenants/enable/{tenantName}")]
    [Headers(AuthorizationBearer, RequestedWithXmlHttpRequest)]
    Task<ApiResponse<string>> EnableAsync(string tenantName);

    /// <summary>
    /// Create or update a content item in Orchard Core.
    /// </summary>
    /// <param name="model">
    /// The necessary parameters to create or update a content item: ContentType, Content etc.
    /// </param>
    /// <returns>The response of the content item creation.</returns>
    [Post("/api/content")]
    [Headers(AuthorizationBearer, RequestedWithXmlHttpRequest)]
    Task<ApiResponse<string>> CreateOrUpdateContentItemAsync([Body(buffered: true)] ContentItem model);

    /// <summary>
    /// Get a previously created content item in Orchard Core.
    /// </summary>
    /// <param name="contentItemId">The necessary parameter to get a content item.</param>
    /// <returns>The response of the content item query.</returns>
    [Get("/api/content/{contentItemId}")]
    [Headers(AuthorizationBearer, RequestedWithXmlHttpRequest)]
    Task<ApiResponse<string>> GetContentItemAsync(string contentItemId);

    /// <summary>
    /// Remove a previously created content item in Orchard Core.
    /// </summary>
    /// <param name="contentItemId">The necessary parameter to remove a content item.</param>
    /// <returns>The response of the content item removal.</returns>
    [Delete("/api/content/{contentItemId}")]
    [Headers(AuthorizationBearer, RequestedWithXmlHttpRequest)]
    Task<ApiResponse<string>> RemoveContentItemAsync(string contentItemId);
}
