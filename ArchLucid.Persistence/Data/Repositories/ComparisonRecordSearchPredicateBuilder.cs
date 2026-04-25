using Dapper;

namespace ArchLucid.Persistence.Data.Repositories;

/// <summary>
///     Shared WHERE clauses and parameters for comparison record search (skip vs cursor paging).
/// </summary>
internal static class ComparisonRecordSearchPredicateBuilder
{
    public static void AppendFilters(
        List<string> conditions,
        DynamicParameters parameters,
        string? comparisonType,
        string? leftRunId,
        string? rightRunId,
        DateTime? createdFromUtc,
        DateTime? createdToUtc,
        string? leftExportRecordId,
        string? rightExportRecordId,
        string? label,
        IReadOnlyList<string>? tags)
    {
        if (!string.IsNullOrWhiteSpace(comparisonType))
        {
            conditions.Add("ComparisonType = @ComparisonType");
            parameters.Add("@ComparisonType", comparisonType);
        }

        if (!string.IsNullOrWhiteSpace(leftRunId))
        {
            conditions.Add("LeftRunId = @LeftRunId");
            parameters.Add("@LeftRunId", leftRunId);
        }

        if (!string.IsNullOrWhiteSpace(rightRunId))
        {
            conditions.Add("RightRunId = @RightRunId");
            parameters.Add("@RightRunId", rightRunId);
        }

        if (createdFromUtc is not null)
        {
            conditions.Add("CreatedUtc >= @CreatedFromUtc");
            parameters.Add("@CreatedFromUtc", createdFromUtc);
        }

        if (createdToUtc is not null)
        {
            conditions.Add("CreatedUtc <= @CreatedToUtc");
            parameters.Add("@CreatedToUtc", createdToUtc);
        }

        if (!string.IsNullOrWhiteSpace(leftExportRecordId))
        {
            conditions.Add("LeftExportRecordId = @LeftExportRecordId");
            parameters.Add("@LeftExportRecordId", leftExportRecordId);
        }

        if (!string.IsNullOrWhiteSpace(rightExportRecordId))
        {
            conditions.Add("RightExportRecordId = @RightExportRecordId");
            parameters.Add("@RightExportRecordId", rightExportRecordId);
        }

        if (!string.IsNullOrWhiteSpace(label))
        {
            conditions.Add("Label = @Label");
            parameters.Add("@Label", label);
        }

        if (tags is not { Count: > 0 })
            return;

        for (int i = 0; i < tags.Count; i++)
        {
            string t = tags[i];
            if (string.IsNullOrWhiteSpace(t))
                continue;
            string paramName = $"@Tag{i}";
            parameters.Add(paramName, t);
            conditions.Add(
                $"EXISTS (SELECT 1 FROM OPENJSON(ISNULL(Tags, '[]')) AS t WHERE t.value = {paramName})");
        }
    }
}
