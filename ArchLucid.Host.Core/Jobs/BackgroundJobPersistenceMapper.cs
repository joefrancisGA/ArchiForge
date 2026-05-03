using ArchLucid.Application.Jobs;
using ArchLucid.Persistence.Data.Repositories;

namespace ArchLucid.Host.Core.Jobs;

internal static class BackgroundJobPersistenceMapper
{
    public static BackgroundJobInfo? ToInfo(BackgroundJobRow? row)
    {
        if (row is null)
            return null;

        if (!Enum.TryParse(row.State, ignoreCase: true, out BackgroundJobState state))
            state = BackgroundJobState.Failed;

        return new BackgroundJobInfo(
            JobId: row.JobId,
            State: state,
            CreatedUtc: row.CreatedUtc,
            StartedUtc: row.StartedUtc,
            CompletedUtc: row.CompletedUtc,
            Error: row.Error,
            FileName: row.FileName,
            ContentType: row.ContentType,
            RetryCount: row.RetryCount,
            MaxRetries: row.MaxRetries);
    }
}
