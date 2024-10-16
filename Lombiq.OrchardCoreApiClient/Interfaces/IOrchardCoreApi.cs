using System.Diagnostics.CodeAnalysis;

namespace Lombiq.OrchardCoreApiClient.Interfaces;

/// <summary>
/// Base interface for representing APIs of Orchard Core.
/// </summary>
// Keep this and the inherited interfaces up-to-date with changes introduced in Orchard Core updates.
[SuppressMessage(
    "Design",
    "CA1040:Avoid empty interfaces",
    Justification = "This is a base interface used to identify all Orchard Core API interfaces May be extended in the future.")]
public interface IOrchardCoreApi
{
}
