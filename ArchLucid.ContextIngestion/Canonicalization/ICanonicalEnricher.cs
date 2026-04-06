using ArchiForge.ContextIngestion.Models;

namespace ArchiForge.ContextIngestion.Canonicalization;

public interface ICanonicalEnricher
{
    IReadOnlyList<CanonicalObject> Enrich(IEnumerable<CanonicalObject> items);
}
