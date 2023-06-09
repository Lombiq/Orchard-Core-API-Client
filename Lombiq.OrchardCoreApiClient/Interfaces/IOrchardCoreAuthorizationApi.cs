using Lombiq.OrchardCoreApiClient.Models;
using Refit;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lombiq.OrchardCoreApiClient.Interfaces;

/// <summary>
/// Interface representing the OpenID APIs of Orchard Core.
/// </summary>
public interface IOrchardCoreAuthorizationApi
{
    /// <summary>
    /// Makes a request to the token endpoint of the Authorization server.
    /// </summary>
    /// <param name="openIdConnectRequestParameters">
    /// Recommended Parameters: grant_type = "client_credentials", client_id, client_secret.
    /// </param>
    /// <returns>The token that can be passed to Orchard Core to validate the request.</returns>
    [Post("/connect/token")]
    Task<Token> TokenAsync(
        [Body(BodySerializationMethod.UrlEncoded)]
        IDictionary<string, string> openIdConnectRequestParameters);
}
