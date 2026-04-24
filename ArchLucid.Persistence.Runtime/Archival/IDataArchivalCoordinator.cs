namespace ArchLucid.Persistence.Archival;

/// <summary>
///     Applies retention cutoffs to persistence stores (soft <c>ArchivedUtc</c> flags).
/// </summary>
public interface IDataArchivalCoordinator
{
    /// <summary>Runs one archival pass using the supplied effective options snapshot.</summary>
    Task RunOnceAsync(DataArchivalOptions options, CancellationToken ct);
}
