namespace ArchiForge.Persistence.Transactions;

public interface IArchiForgeUnitOfWorkFactory
{
    Task<IArchiForgeUnitOfWork> CreateAsync(CancellationToken ct);
}
