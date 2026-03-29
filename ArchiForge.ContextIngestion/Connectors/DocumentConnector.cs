using ArchiForge.ContextIngestion.Contracts;
using ArchiForge.ContextIngestion.Interfaces;
using ArchiForge.ContextIngestion.Models;

namespace ArchiForge.ContextIngestion.Connectors;

/// <summary>
/// Normalizes inline <see cref="ContextDocumentReference"/> items using the first
/// <see cref="IContextDocumentParser"/> where <see cref="IContextDocumentParser.CanParse"/> returns true
/// (registration order of <see cref="IEnumerable{IContextDocumentParser}"/> in DI when multiple parsers match).
/// </summary>
public class DocumentConnector(IEnumerable<IContextDocumentParser> parsers) : IContextConnector
{
    public string ConnectorType => "documents";

    public Task<RawContextPayload> FetchAsync(
        ContextIngestionRequest request,
        CancellationToken ct)
    {
        _ = ct;
        return Task.FromResult(new RawContextPayload
        {
            Documents = request.Documents.ToList()
        });
    }

    public async Task<NormalizedContextBatch> NormalizeAsync(
        RawContextPayload payload,
        CancellationToken ct)
    {
        NormalizedContextBatch batch = new();

        foreach (ContextDocumentReference document in payload.Documents)
        {
            IContextDocumentParser? parser = parsers.FirstOrDefault(x => x.CanParse(document.ContentType));
            if (parser is null)
            {
                batch.Warnings.Add(
                    $"No registered context document parser accepted '{document.Name}' " +
                    $"(contentType='{document.ContentType}'). Document skipped. " +
                    "For HTTP requests, ContentType is validated at the API; if you still see this, " +
                    "align IContextDocumentParser registrations with SupportedContextDocumentContentTypes " +
                    "or check non-API callers building ContextIngestionRequest.");
                continue;
            }

            IReadOnlyList<CanonicalObject> objects = await parser.ParseAsync(document, ct);
            batch.CanonicalObjects.AddRange(objects);
        }

        return batch;
    }

    public Task<ContextDelta> DeltaAsync(
        NormalizedContextBatch current,
        ContextSnapshot? previous,
        CancellationToken ct)
    {
        _ = current;
        _ = ct;
        return Task.FromResult(new ContextDelta
        {
            Summary = previous is null ? "Initial document ingestion" : "Updated document ingestion"
        });
    }
}
