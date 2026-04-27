namespace ArchLucid.Persistence.Pilots;

public interface IPilotBaselineRepository
{
    Task<PilotBaselineRecord?> GetAsync(Guid tenantId, CancellationToken cancellationToken);

    Task UpsertAsync(PilotBaselineRecord record, CancellationToken cancellationToken);
}
