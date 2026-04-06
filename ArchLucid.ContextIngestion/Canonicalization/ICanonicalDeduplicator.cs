using ArchiForge.ContextIngestion.Models;

namespace ArchiForge.ContextIngestion.Canonicalization;

public interface ICanonicalDeduplicator
{
    IReadOnlyList<CanonicalObject> Deduplicate(
        IEnumerable<CanonicalObject> items);
}
