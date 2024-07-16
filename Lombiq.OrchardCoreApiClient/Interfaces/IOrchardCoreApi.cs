using System.Diagnostics.CodeAnalysis;

namespace Lombiq.OrchardCoreApiClient.Interfaces;

/// <summary>
/// Marker interface for representing the subset of APIs of Orchard Core.
/// </summary>
[SuppressMessage(
    "Design",
    "CA1040:Avoid empty interfaces",
    Justification = "This is a marker interface used to identify all Orchard Core API interfaces.")]
public interface IOrchardCoreApi
{
}
