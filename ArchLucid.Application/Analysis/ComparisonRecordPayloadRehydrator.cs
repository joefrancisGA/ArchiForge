using System.Text.Json;

using ArchLucid.Contracts.Metadata;

namespace ArchLucid.Application.Analysis;

public static class ComparisonRecordPayloadRehydrator
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public static EndToEndReplayComparisonReport? RehydrateEndToEnd(
        ComparisonRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (string.IsNullOrWhiteSpace(record.PayloadJson))
            return null;

        try
        {
            return JsonSerializer.Deserialize<EndToEndReplayComparisonReport>(record.PayloadJson, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Comparison record '{record.ComparisonRecordId}' PayloadJson could not be deserialized as EndToEndReplayComparisonReport. " +
                "The stored JSON may be corrupt.", ex);
        }
    }

    public static ExportRecordDiffResult? RehydrateExportDiff(
        ComparisonRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (string.IsNullOrWhiteSpace(record.PayloadJson))
            return null;

        try
        {
            return JsonSerializer.Deserialize<ExportRecordDiffResult>(record.PayloadJson, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Comparison record '{record.ComparisonRecordId}' PayloadJson could not be deserialized as ExportRecordDiffResult. " +
                "The stored JSON may be corrupt.", ex);
        }
    }
}
