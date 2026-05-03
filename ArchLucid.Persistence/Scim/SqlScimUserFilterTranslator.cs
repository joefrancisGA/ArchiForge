using ArchLucid.Core.Scim.Filtering;

using Dapper;

namespace ArchLucid.Persistence.Scim;

internal static class SqlScimUserFilterTranslator
{
    public static string BuildWhere(ScimFilterNode? filter, DynamicParameters parameters, ref int nextParam)
    {
        if (filter is null)
            return "1 = 1";

        return Build(filter, parameters, ref nextParam);
    }

    private static string Build(ScimFilterNode node, DynamicParameters parameters, ref int nextParam)
    {
        switch (node)
        {
            case ScimNotNode n:
                return $"NOT ({Build(n.Inner, parameters, ref nextParam)})";

            case ScimAndNode a:
                return $"({Build(a.Left, parameters, ref nextParam)}) AND ({Build(a.Right, parameters, ref nextParam)})";

            case ScimOrNode o:
                return $"({Build(o.Left, parameters, ref nextParam)}) OR ({Build(o.Right, parameters, ref nextParam)})";

            case ScimPresentNode p:
                return $"{ColumnSql(p.AttributePath)} IS NOT NULL";

            case ScimComparisonNode c:
                return BuildComparison(c, parameters, ref nextParam);

            default:
                throw new InvalidOperationException($"Unsupported filter node {node.GetType().Name}.");
        }
    }

    private static string BuildComparison(ScimComparisonNode c, DynamicParameters parameters, ref int nextParam)
    {
        string col = ColumnSql(c.AttributePath);
        string op = c.Operator.ToLowerInvariant();

        if (string.Equals(c.AttributePath, "active", StringComparison.OrdinalIgnoreCase))
        {
            bool bit = string.Equals(c.Value, "true", StringComparison.OrdinalIgnoreCase);
            string name = NextName(ref nextParam);
            parameters.Add(name, bit ? 1 : 0);

            return op switch
            {
                "eq" => $"{col} = @{name}",
                "ne" => $"{col} <> @{name}",
                "gt" => $"{col} > @{name}",
                "lt" => $"{col} < @{name}",
                "ge" => $"{col} >= @{name}",
                "le" => $"{col} <= @{name}",
                _ => throw new ScimFilterSqlException($"Unsupported operator '{op}' for active.")
            };
        }

        string pName = NextName(ref nextParam);
        string bound = op switch
        {
            "co" => $"%{c.Value}%",
            "sw" => $"{c.Value}%",
            "ew" => $"%{c.Value}",
            _ => c.Value
        };

        parameters.Add(pName, bound);

        return op switch
        {
            "eq" => $"{col} = @{pName}",
            "ne" => $"{col} <> @{pName}",
            "co" => $"{col} LIKE @{pName}",
            "sw" => $"{col} LIKE @{pName}",
            "ew" => $"{col} LIKE @{pName}",
            "gt" => $"{col} > @{pName}",
            "lt" => $"{col} < @{pName}",
            "ge" => $"{col} >= @{pName}",
            "le" => $"{col} <= @{pName}",
            _ => throw new ScimFilterSqlException($"Unsupported operator '{op}'.")
        };
    }

    private static string NextName(ref int nextParam)
    {
        nextParam++;

        return $"f{nextParam}";
    }

    private static string ColumnSql(string attributePath)
    {
        string p = attributePath.Trim();

        if (string.Equals(p, "userName", StringComparison.OrdinalIgnoreCase))
            return "u.UserName";

        if (string.Equals(p, "displayName", StringComparison.OrdinalIgnoreCase))
            return "u.DisplayName";

        if (ScimKnownUserFilterPaths.IsEmailsWorkValuePath(p))
            return "u.UserName";

        if (string.Equals(p, "externalId", StringComparison.OrdinalIgnoreCase))
            return "u.ExternalId";

        if (string.Equals(p, "active", StringComparison.OrdinalIgnoreCase))
            return "u.Active";

        if (string.Equals(p, "id", StringComparison.OrdinalIgnoreCase))
            return "CAST(u.Id AS NVARCHAR(36))";

        throw new ScimFilterSqlException($"Unknown attribute '{attributePath}' for SQL translation.");
    }
}
