namespace Lombiq.OrchardCoreApiClient.Models;

/// <summary>
/// Models inheriting from this contain information about the request URL.
/// </summary>
public abstract class RequestUrlHolderBase
{
    public string RequestUrlHost { get; set; }

    // The setter should automatically trim leading or trailing slashes to prevent "The url prefix can not contain more
    // than one segment." error.
    public string RequestUrlPrefix { get; set => field = value?.Trim().Trim('/'); }
}
