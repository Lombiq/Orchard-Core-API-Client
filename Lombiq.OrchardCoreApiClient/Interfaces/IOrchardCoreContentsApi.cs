using OrchardCore.ContentManagement;
using Refit;
using System.Threading.Tasks;
using static Lombiq.OrchardCoreApiClient.Constants.CommonHeaders;

namespace Lombiq.OrchardCoreApiClient.Interfaces;

/// <summary>
/// Interface representing the contents API of Orchard Core.
/// </summary>
public interface IOrchardCoreContentsApi : IOrchardCoreApi
{
    /// <summary>
    /// Create or update a content item in Orchard Core.
    /// </summary>
    /// <param name="model">
    /// The necessary parameters to create or update a content item: ContentType, Content etc.
    /// </param>
    /// <returns>The response of the content item creation.</returns>
    [Post("/api/content")]
    [Headers(AuthorizationBearer, RequestedWithXmlHttpRequest)]
    Task<ApiResponse<string>> CreateOrUpdateAsync([Body(buffered: true)] ContentItem model);

    /// <summary>
    /// Get a previously created content item in Orchard Core.
    /// </summary>
    /// <param name="contentItemId">The necessary parameter to get a content item.</param>
    /// <returns>The response of the content item query.</returns>
    [Get("/api/content/{contentItemId}")]
    [Headers(AuthorizationBearer, RequestedWithXmlHttpRequest)]
    Task<ApiResponse<string>> GetAsync(string contentItemId);

    /// <summary>
    /// Remove a previously created content item in Orchard Core.
    /// </summary>
    /// <param name="contentItemId">The necessary parameter to remove a content item.</param>
    /// <returns>The response of the content item removal.</returns>
    [Delete("/api/content/{contentItemId}")]
    [Headers(AuthorizationBearer, RequestedWithXmlHttpRequest)]
    Task<ApiResponse<string>> RemoveAsync(string contentItemId);
}
