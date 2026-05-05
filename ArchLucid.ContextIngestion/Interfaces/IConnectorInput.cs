using ArchLucid.ContextIngestion.Models;

namespace ArchLucid.ContextIngestion.Interfaces;

/// <summary>
///     Extracts a typed payload slice from <see cref="ContextIngestionRequest" /> for one connector slot.
/// </summary>
public interface IConnectorInput<out TPayload>
    where TPayload : class
{
    TPayload Extract(ContextIngestionRequest request);
}
