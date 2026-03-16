using System.Text.Json;
using ArchiForge.Contracts.Metadata;

namespace ArchiForge.Application.Analysis;

public static class ComparisonRecordPayloadRehydrator
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public static EndToEndReplayComparisonReport? RehydrateEndToEnd(
        ComparisonRecord record)
    {
        if (string.IsNullOrWhiteSpace(record.PayloadJson))
        {
            return null;
        }

        return JsonSerializer.Deserialize<EndToEndReplayComparisonReport>(
            record.PayloadJson,
            JsonOptions);
    }

    public static ExportRecordDiffResult? RehydrateExportDiff(
        ComparisonRecord record)
    {
        if (string.IsNullOrWhiteSpace(record.PayloadJson))
        {
            return null;
        }

        return JsonSerializer.Deserialize<ExportRecordDiffResult>(
            record.PayloadJson,
            JsonOptions);
    }
}

