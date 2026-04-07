using ArchLucid.Core.Transactions;

namespace ArchLucid.Persistence.Transactions;

/// <summary>
/// Returns new <see cref="InMemoryArchLucidUnitOfWork"/> instances for tests and in-memory API mode.
/// </summary>
public sealed class InMemoryArchLucidUnitOfWorkFactory : IArchLucidUnitOfWorkFactory
{
    /// <inheritdoc />
    public Task<IArchLucidUnitOfWork> CreateAsync(CancellationToken ct)
    {
        _ = ct;
        return Task.FromResult<IArchLucidUnitOfWork>(new InMemoryArchLucidUnitOfWork());
    }
}
