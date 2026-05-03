using ArchLucid.Host.Core.Configuration;

namespace ArchLucid.Host.Core.Startup.Validation.Rules;

internal static class BatchReplayRules
{
    public static void Collect(IConfiguration configuration, List<string> errors)
    {
        BatchReplayOptions batch =
            configuration.GetSection(BatchReplayOptions.SectionName).Get<BatchReplayOptions>() ?? new BatchReplayOptions();

        const int min = 1;
        const int max = 500;

        if (batch.MaxComparisonRecordIds is < min or > max)

            errors.Add(
                $"ComparisonReplay:Batch:MaxComparisonRecordIds must be between {min} and {max} (inclusive).");
    }
}
