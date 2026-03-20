namespace ArchiForge.Persistence.Transactions;

public sealed class InMemoryArchiForgeUnitOfWorkFactory : IArchiForgeUnitOfWorkFactory
{
    public Task<IArchiForgeUnitOfWork> CreateAsync(CancellationToken ct)
    {
        _ = ct;
        return Task.FromResult<IArchiForgeUnitOfWork>(new InMemoryArchiForgeUnitOfWork());
    }
}
