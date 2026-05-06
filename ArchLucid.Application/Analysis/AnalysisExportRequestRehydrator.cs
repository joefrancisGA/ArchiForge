using System.Text.Json;
using ArchLucid.Contracts.Metadata;

namespace ArchLucid.Application.Analysis;
/// <summary>
///     Rehydrates a <see cref = "PersistedAnalysisExportRequest"/> from the JSON stored on a <see cref = "RunExportRecord"/>
///     .
///     Returns <see langword="null"/> when no analysis request JSON is present (e.g. the record pre-dates this feature).
///     Wraps <see cref = "JsonException"/> in an <see cref = "InvalidOperationException"/> so callers receive a clear
///     diagnostic message when stored JSON is corrupt or was written by an incompatible schema version.
/// </summary>
public static class AnalysisExportRequestRehydrator
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };
    public static ArchLucid.Application.Analysis.PersistedAnalysisExportRequest? Rehydrate(RunExportRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        if (string.IsNullOrWhiteSpace(record.AnalysisRequestJson))
            return null;
        try
        {
            return JsonSerializer.Deserialize<PersistedAnalysisExportRequest>(record.AnalysisRequestJson, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Export record '{record.ExportRecordId}' AnalysisRequestJson could not be deserialized. " + "The stored JSON may be corrupt or written by an incompatible schema version.", ex);
        }
    }
}