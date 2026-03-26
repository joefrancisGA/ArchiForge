using ArchiForge.ContextIngestion.Infrastructure;
using ArchiForge.ContextIngestion.Interfaces;
using ArchiForge.ContextIngestion.Models;

namespace ArchiForge.ContextIngestion.Connectors;

public class InfrastructureDeclarationConnector(IEnumerable<IInfrastructureDeclarationParser> parsers)
    : IContextConnector
{
    public string ConnectorType => "infrastructure-declarations";

    public Task<RawContextPayload> FetchAsync(
        ContextIngestionRequest request,
        CancellationToken ct)
    {
        _ = ct;
        return Task.FromResult(new RawContextPayload
        {
            InfrastructureDeclarations = request.InfrastructureDeclarations.ToList()
        });
    }

    public async Task<NormalizedContextBatch> NormalizeAsync(
        RawContextPayload payload,
        CancellationToken ct)
    {
        NormalizedContextBatch batch = new();

        foreach (InfrastructureDeclarationReference declaration in payload.InfrastructureDeclarations)
        {
            IInfrastructureDeclarationParser? parser = parsers.FirstOrDefault(x => x.CanParse(declaration.Format));
            if (parser is null)
            {
                batch.Warnings.Add(
                    $"No infrastructure declaration parser for '{declaration.Name}' (format='{declaration.Format}'). Declaration skipped.");
                continue;
            }

            IReadOnlyList<CanonicalObject> objects = await parser.ParseAsync(declaration, ct);
            batch.CanonicalObjects.AddRange(objects);
        }

        return batch;
    }

    public Task<ContextDelta> DeltaAsync(
        NormalizedContextBatch current,
        ContextSnapshot? previous,
        CancellationToken ct)
    {
        _ = ct;

        int currentCount = current.CanonicalObjects.Count;

        if (previous is null)
        {
            return Task.FromResult(new ContextDelta
            {
                Summary = $"Initial infrastructure declaration ingestion: {currentCount} object(s)."
            });
        }

        int previousCount = previous.CanonicalObjects.Count;
        int diff = currentCount - previousCount;

        string summary = diff == 0
            ? $"Infrastructure declaration ingestion: {currentCount} object(s), no count change."
            : $"Infrastructure declaration ingestion: {currentCount} object(s) (\u0394{diff:+#;-#;0} from prior snapshot).";

        return Task.FromResult(new ContextDelta { Summary = summary });
    }
}
