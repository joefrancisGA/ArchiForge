using System.Collections.Concurrent;

using ArchLucid.Core.Pilots;

namespace ArchLucid.Persistence.Pilots;

public sealed class InMemoryPilotCloseoutRepository : IPilotCloseoutRepository
{
    private readonly ConcurrentBag<PilotCloseoutRecord> _rows = [];

    public Task InsertAsync(PilotCloseoutRecord record, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(record);
        _rows.Add(record);

        return Task.CompletedTask;
    }
}
