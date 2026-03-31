namespace ArchiForge.Api.Services;

/// <summary>Produces a single path segment safe for <see cref="System.IO.Compression.ZipArchive"/> entry names.</summary>
public static class BatchReplayZipPathSanitizer
{
    public static string FolderForComparisonRecordId(string comparisonRecordId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(comparisonRecordId);

        char[] invalid = Path.GetInvalidFileNameChars();

        return new string(comparisonRecordId
            .Select(c => c is '/' or '\\' || Array.IndexOf(invalid, c) >= 0 ? '_' : c)
            .ToArray());
    }
}
