using JetBrains.Annotations;

namespace ArchiForge.Persistence.Sql;

public interface ISchemaBootstrapper
{
    Task EnsureSchemaAsync(CancellationToken ct);
    [UsedImplicitly]
    IReadOnlyList<string> SplitGoBatches(string script);
}
