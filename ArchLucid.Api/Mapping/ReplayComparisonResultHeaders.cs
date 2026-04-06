using ArchiForge.Api.Http;
using ArchiForge.Application.Analysis;

namespace ArchiForge.Api.Mapping;

internal static class ReplayComparisonResultHeaders
{
    /// <summary>Headers returned with full replay artifact responses.</summary>
    public static void ApplyFull(HttpResponse response, ReplayComparisonResult result)
    {
        response.Headers[ArchiForgeHttpHeaders.ComparisonRecordId] = result.ComparisonRecordId;
        response.Headers[ArchiForgeHttpHeaders.ComparisonType] = result.ComparisonType;
        response.Headers[ArchiForgeHttpHeaders.ReplayMode] = result.ReplayMode;
        response.Headers[ArchiForgeHttpHeaders.VerificationPassed] = result.VerificationPassed.ToString();
        if (result.VerificationMessage is { } msg)
            response.Headers[ArchiForgeHttpHeaders.VerificationMessage] = msg;
        ApplyOptionalIdentifiers(response, result);
    }

    /// <summary>Subset of headers for metadata-only replay responses.</summary>
    public static void ApplyMetadata(HttpResponse response, ReplayComparisonResult result) =>
        ApplyOptionalIdentifiers(response, result);

    private static void ApplyOptionalIdentifiers(HttpResponse response, ReplayComparisonResult result)
    {
        if (result.LeftRunId is { } leftRunId)
            response.Headers[ArchiForgeHttpHeaders.LeftRunId] = leftRunId;
        if (result.RightRunId is { } rightRunId)
            response.Headers[ArchiForgeHttpHeaders.RightRunId] = rightRunId;
        if (result.LeftExportRecordId is { } leftExportId)
            response.Headers[ArchiForgeHttpHeaders.LeftExportRecordId] = leftExportId;
        if (result.RightExportRecordId is { } rightExportId)
            response.Headers[ArchiForgeHttpHeaders.RightExportRecordId] = rightExportId;
        if (result.CreatedUtc is { } createdUtc)
            response.Headers[ArchiForgeHttpHeaders.CreatedUtc] = createdUtc.ToString("O");
        if (result.FormatProfile is { } formatProfile)
            response.Headers[ArchiForgeHttpHeaders.FormatProfile] = formatProfile;
        if (result.PersistedReplayRecordId is { } persistedId)
            response.Headers[ArchiForgeHttpHeaders.PersistedReplayRecordId] = persistedId;
    }
}
