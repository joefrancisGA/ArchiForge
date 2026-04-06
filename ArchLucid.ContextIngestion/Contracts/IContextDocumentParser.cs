using ArchiForge.ContextIngestion.Models;

namespace ArchiForge.ContextIngestion.Contracts;

public interface IContextDocumentParser
{
    bool CanParse(string contentType);

    Task<IReadOnlyList<CanonicalObject>> ParseAsync(
        ContextDocumentReference document,
        CancellationToken ct);
}
