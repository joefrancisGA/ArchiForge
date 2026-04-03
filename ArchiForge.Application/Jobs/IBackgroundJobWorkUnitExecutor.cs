namespace ArchiForge.Application.Jobs;

/// <summary>Runs a <see cref="BackgroundJobWorkUnit"/> to produce a downloadable export file.</summary>
public interface IBackgroundJobWorkUnitExecutor
{
    Task<BackgroundJobFile> ExecuteAsync(BackgroundJobWorkUnit workUnit, CancellationToken cancellationToken);
}
