namespace ArchLucid.Core.Pilots;

public interface IPilotCloseoutRepository
{
    Task InsertAsync(PilotCloseoutRecord record, CancellationToken cancellationToken);
}
