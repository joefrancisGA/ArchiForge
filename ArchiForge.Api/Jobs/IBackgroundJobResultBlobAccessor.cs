using ArchiForge.Application.Jobs;

namespace ArchiForge.Api.Jobs;

public interface IBackgroundJobResultBlobAccessor
{
    Task<string> UploadAsync(string jobId, BackgroundJobFile file, CancellationToken cancellationToken = default);

    Task<BackgroundJobFile?> DownloadAsync(
        string blobName,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);
}
