using ArchiForge.ContextIngestion.Models;

namespace ArchiForge.ContextIngestion.Infrastructure;

public interface IInfrastructureDeclarationParser
{
    bool CanParse(string format);

    Task<IReadOnlyList<CanonicalObject>> ParseAsync(
        InfrastructureDeclarationReference declaration,
        CancellationToken ct);
}
