namespace ArchiForge.ContextIngestion.Interfaces;

using Models;

public interface IContextConnector
{
    string ConnectorType { get; }

    Task<RawContextPayload> FetchAsync(
        ContextIngestionRequest request,
        CancellationToken ct);

    Task<NormalizedContextBatch> NormalizeAsync(
        RawContextPayload payload,
        CancellationToken ct);

    Task<ContextDelta> DeltaAsync(
        NormalizedContextBatch current,
        ContextSnapshot? previous,
        CancellationToken ct);
}

