namespace ArchiForge.Persistence.Transactions;

/// <summary>
/// Returns new <see cref="InMemoryArchiForgeUnitOfWork"/> instances for tests and in-memory API mode.
/// </summary>
public sealed class InMemoryArchiForgeUnitOfWorkFactory : IArchiForgeUnitOfWorkFactory
{
    /// <inheritdoc />
    public Task<IArchiForgeUnitOfWork> CreateAsync(CancellationToken ct)
    {
        _ = ct;
        return Task.FromResult<IArchiForgeUnitOfWork>(new InMemoryArchiForgeUnitOfWork());
    }
}
