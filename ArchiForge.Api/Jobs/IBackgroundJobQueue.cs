namespace ArchiForge.Api.Jobs;

public interface IBackgroundJobQueue
{
    string Enqueue(string? fileNameHint, string? contentTypeHint, Func<CancellationToken, Task<BackgroundJobFile>> work);
    BackgroundJobInfo? GetInfo(string jobId);
    BackgroundJobFile? GetFile(string jobId);
}

