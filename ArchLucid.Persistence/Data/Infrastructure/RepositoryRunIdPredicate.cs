using System.Globalization;

using Dapper;

namespace ArchLucid.Persistence.Data.Infrastructure;

/// <summary>
///     Matches persisted authority run identifiers when callers supply GUID-equivalent string formats (for example N vs D).
/// </summary>
internal static class RepositoryRunIdPredicate
{
    internal static string WhereClauseMatching(string columnName)
    {
        if (string.IsNullOrWhiteSpace(columnName))
            throw new ArgumentException("Column name is required.", nameof(columnName));

        return string.Format(
            CultureInfo.InvariantCulture,
            """
            (
                (@RunIdGuid IS NOT NULL AND TRY_CONVERT(UNIQUEIDENTIFIER, {0}) = @RunIdGuid)
                OR (@RunIdGuid IS NULL AND {0} = @RunId)
            )
            """,
            columnName);
    }

    internal static void AddRunIdMatchParameters(DynamicParameters parameters, string runId)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        parameters.Add("RunId", runId);

        Guid? runGuid = Guid.TryParse(runId, out Guid parsed) ? parsed : null;

        parameters.Add("RunIdGuid", runGuid);
    }
}
