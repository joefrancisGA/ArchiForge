using ArchLucid.Core.Scim.Models;

namespace ArchLucid.Core.Scim.Filtering;

public static class ScimFilterInMemoryEvaluator
{
    public static bool Matches(ScimUserRecord user, ScimFilterNode? filter)
    {
        return filter is null || Evaluate(user, filter);
    }

    private static bool Evaluate(ScimUserRecord user, ScimFilterNode node)
    {
        switch (node)
        {
            case ScimNotNode n:
                return !Evaluate(user, n.Inner);

            case ScimAndNode a:
                return Evaluate(user, a.Left) && Evaluate(user, a.Right);

            case ScimOrNode o:
                return Evaluate(user, o.Left) || Evaluate(user, o.Right);

            case ScimPresentNode p:
                return GetString(user, p.AttributePath) is not null;

            case ScimComparisonNode c:
                return Compare(user, c);

            default:
                return false;
        }
    }

    private static bool Compare(ScimUserRecord user, ScimComparisonNode c)
    {
        string? left = GetString(user, c.AttributePath);

        if (string.Equals(c.Operator, "pr", StringComparison.Ordinal))
            return left is not null;

        string right = c.Value;

        return c.Operator.ToLowerInvariant() switch
        {
            "eq" => string.Equals(left, right, StringComparison.OrdinalIgnoreCase),
            "ne" => !string.Equals(left, right, StringComparison.OrdinalIgnoreCase),
            "co" => left is not null && left.Contains(right, StringComparison.OrdinalIgnoreCase),
            "sw" => left is not null && left.StartsWith(right, StringComparison.OrdinalIgnoreCase),
            "ew" => left is not null && left.EndsWith(right, StringComparison.OrdinalIgnoreCase),
            "gt" => LexGreater(left, right),
            "lt" => LexLess(left, right),
            "ge" => LexGreater(left, right) || string.Equals(left, right, StringComparison.OrdinalIgnoreCase),
            "le" => LexLess(left, right) || string.Equals(left, right, StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    private static bool LexGreater(string? left, string right)
    {
        if (left is null)
            return false;

        return string.Compare(left, right, StringComparison.OrdinalIgnoreCase) > 0;
    }

    private static bool LexLess(string? left, string right)
    {
        if (left is null)
            return false;

        return string.Compare(left, right, StringComparison.OrdinalIgnoreCase) < 0;
    }

    private static string? GetString(ScimUserRecord user, string path)
    {
        string p = path.Trim();

        if (string.Equals(p, "userName", StringComparison.OrdinalIgnoreCase))
            return user.UserName;

        if (string.Equals(p, "displayName", StringComparison.OrdinalIgnoreCase))
            return user.DisplayName;

        if (string.Equals(p, "externalId", StringComparison.OrdinalIgnoreCase))
            return user.ExternalId;

        if (string.Equals(p, "active", StringComparison.OrdinalIgnoreCase))
            return user.Active ? "true" : "false";

        return string.Equals(p, "id", StringComparison.OrdinalIgnoreCase) ? user.Id.ToString("D") : null;
    }
}
