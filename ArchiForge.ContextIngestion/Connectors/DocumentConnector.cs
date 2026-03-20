using ArchiForge.ContextIngestion.Contracts;
using ArchiForge.ContextIngestion.Interfaces;
using ArchiForge.ContextIngestion.Models;

namespace ArchiForge.ContextIngestion.Connectors;

public class DocumentConnector : IContextConnector
{
    private readonly IEnumerable<IContextDocumentParser> _parsers;

    public DocumentConnector(IEnumerable<IContextDocumentParser> parsers)
    {
        _parsers = parsers;
    }

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
        var batch = new NormalizedContextBatch();

        foreach (var document in payload.Documents)
        {
            var parser = _parsers.FirstOrDefault(x => x.CanParse(document.ContentType));
            if (parser is null)
            {
                batch.Warnings.Add(
                    $"No context document parser for document '{document.Name}' " +
                    $"(contentType='{document.ContentType}'). Document skipped.");
                continue;
            }

            var objects = await parser.ParseAsync(document, ct);
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
