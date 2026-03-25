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
        _ = current;
        _ = ct;
        return Task.FromResult(new ContextDelta
        {
            Summary = previous is null
                ? "Initial infrastructure declaration ingestion"
                : "Updated infrastructure declaration ingestion"
        });
    }
}
