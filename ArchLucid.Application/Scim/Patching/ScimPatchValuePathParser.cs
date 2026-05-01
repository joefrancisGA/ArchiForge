using System.Globalization;

namespace ArchLucid.Application.Scim.Patching;

/// <summary>
/// RFC 7644 §3.5.2 — minimal <c>valuePath</c> support: <c>attrPath "[" valFilter "]" ["." subAttr]</c>.
/// Production interop focuses on Entra-style <c>members[value eq "{uuid}"]</c> (+ optional <c>.active</c>).
/// </summary>
public static class ScimPatchValuePathParser
{
    /// <summary>Interprets a path for SCIM Group membership PATCH handlers.</summary>
    public static ScimPatchPathParseOutcome ParseForGroupMemberPath(string path)
    {
        if (path is null) throw new ArgumentNullException(nameof(path));

        string trimmed = path.Trim();

        if (trimmed.Length is 0)
            return new ScimPatchPathInvalidOutcome("Empty path.");

        if (!trimmed.Contains('[', StringComparison.Ordinal))
        {
            if (trimmed.Equals("members", StringComparison.OrdinalIgnoreCase))
                return new ScimPatchFlatAttributePathOutcome("members");

            return new ScimPatchPathInvalidOutcome(
                "Only path 'members' or members[value eq \"...\"] is supported for group PATCH.");
        }

        try
        {
            return ParseMembersValuePath(trimmed, requireMembersAttribute: true);
        }
        catch (ScimPatchException ex)
        {
            return new ScimPatchPathInvalidOutcome(ex.Message);
        }
    }

    /// <summary>
    /// Interprets a path for User flat PATCH via <see cref="ScimPatchOpEvaluator" />.
    /// Any complex <c>valuePath</c> is rejected (not implemented on this resource).
    /// </summary>
    public static string ParseForUserFlatPatchPath(string path)
    {
        if (path is null) throw new ArgumentNullException(nameof(path));

        string trimmed = path.Trim();

        if (trimmed.Length is 0)
            throw new ScimPatchException("invalidPath", "Empty path.");

        if (trimmed.Equals("members", StringComparison.OrdinalIgnoreCase))
            throw new ScimPatchException("notImplemented", "User resources do not support 'members' in flat PATCH.");

        if (!trimmed.Contains('[', StringComparison.Ordinal))
        {
            if (trimmed.Contains('\"', StringComparison.Ordinal) || trimmed.Contains('(', StringComparison.Ordinal))
                throw new ScimPatchException("invalidPath", "Path contains unsupported characters.");

            return trimmed;
        }

        ScimPatchPathParseOutcome complex = ParseMembersValuePath(trimmed, requireMembersAttribute: false);

        return complex switch
        {
            ScimPatchMembersFilteredPathOutcome =>
                throw new ScimPatchException(
                    "notImplemented",
                    "Complex attribute selectors are not supported on User PATCH."),
            ScimPatchPathNotImplementedOutcome n =>
                throw new ScimPatchException("notImplemented", n.Detail),
            ScimPatchFlatAttributePathOutcome =>
                throw new ScimPatchException("invalidPath", "Malformed path brackets."),
            _ => throw new ScimPatchException("notImplemented", "Complex attribute path is not implemented.")
        };
    }

    private static ScimPatchPathParseOutcome ParseMembersValuePath(string path, bool requireMembersAttribute)
    {
        int open = path.IndexOf('[', StringComparison.Ordinal);

        if (open < 0)
            return new ScimPatchFlatAttributePathOutcome(path);

        int close = path.IndexOf(']', open + 1);

        if (close < 0)
            throw new ScimPatchException("invalidPath", "Unclosed '[' in path.");

        if (path.IndexOf('[', open + 1) >= 0)
            return new ScimPatchPathNotImplementedOutcome("Nested bracket paths are not implemented.");

        ReadOnlySpan<char> attrPath = path.AsSpan(0, open).TrimEnd();

        if (attrPath.Length is 0)
            throw new ScimPatchException("invalidPath", "Missing attribute path before '['.");

        if (requireMembersAttribute && !MemoryExtensions.Equals(attrPath, "members".AsSpan(), StringComparison.OrdinalIgnoreCase))
        {
            return new ScimPatchPathNotImplementedOutcome(
                $"Complex selectors on attribute '{attrPath.ToString()}' are not implemented (only 'members[value eq \"…\"]').");
        }

        if (!requireMembersAttribute &&
            !MemoryExtensions.Equals(attrPath, "members".AsSpan(), StringComparison.OrdinalIgnoreCase))
        {
            ReadOnlySpan<char> filterProbe = path.AsSpan(open + 1, close - open - 1).Trim();

            if (LooksLikeRichScimFilter(filterProbe))
                return new ScimPatchPathNotImplementedOutcome(
                    $"Complex selectors on '{attrPath.ToString()}' are not implemented.");

            if (TryParseValueEqFilter(filterProbe, out _))
                return new ScimPatchPathNotImplementedOutcome(
                    $"Complex selectors on '{attrPath.ToString()}' are not implemented.");

            throw new ScimPatchException(
                "invalidPath",
                "Complex path does not match supported attribute[value eq \"…\"] grammar.");
        }

        ReadOnlySpan<char> filter = path.AsSpan(open + 1, close - open - 1).Trim();

        ReadOnlySpan<char> tail = path.AsSpan(close + 1).Trim();

        string? subAttr = null;

        if (tail.Length > 0)
        {
            if (!tail.StartsWith(".", StringComparison.Ordinal))
                throw new ScimPatchException(
                    "invalidPath",
                    "Unexpected characters after ']' — expected '.' sub-attribute or nothing.");

            ReadOnlySpan<char> rest = tail[1..].Trim();

            if (rest.Length is 0 || rest.IndexOfAny(['.', '[']) >= 0)
                throw new ScimPatchException("invalidPath", "Invalid sub-attribute after '.'.");

            subAttr = rest.ToString();
        }

        if (subAttr is not null && !subAttr.Equals("active", StringComparison.OrdinalIgnoreCase))
        {
            return new ScimPatchPathNotImplementedOutcome(
                $"Sub-attribute '{subAttr}' on group members is not implemented (only '.active').");
        }

        if (!TryParseValueEqFilter(filter, out Guid memberId))
        {
            if (LooksLikeRichScimFilter(filter))
                return new ScimPatchPathNotImplementedOutcome("Only 'value eq \"…\"' member filters are implemented.");

            throw new ScimPatchException(
                "invalidPath",
                "Member filter must be 'value eq \"<guid>\"' (RFC 7644 subset).");
        }

        return new ScimPatchMembersFilteredPathOutcome(memberId, subAttr);
    }

    internal static bool TryParseValueEqFilter(ReadOnlySpan<char> filterTrimmed, out Guid memberId)
    {
        memberId = Guid.Empty;

        if (filterTrimmed.Length is 0)
            return false;

        if (!filterTrimmed.StartsWith("value".AsSpan(), StringComparison.OrdinalIgnoreCase))
            return false;

        ReadOnlySpan<char> rest = filterTrimmed[5..].TrimStart();

        if (!rest.StartsWith("eq".AsSpan(), StringComparison.OrdinalIgnoreCase))
            return false;

        rest = rest[2..].TrimStart();

        if (rest.Length is 0)
            return false;

        if (!TryReadCompValue(rest, out string? raw))
            return false;

        return Guid.TryParse(raw, out memberId);
    }

    private static bool TryReadCompValue(ReadOnlySpan<char> rest, out string? value)
    {
        value = null;

        if (rest.Length is 0)
            return false;

        ReadOnlySpan<char> tail;

        if (rest[0] is '"')
        {
            int i = 1;
            System.Text.StringBuilder sb = new();

            while (i < rest.Length)
            {
                char c = rest[i];

                if (c is '"')
                {
                    if (i + 1 < rest.Length && rest[i + 1] is '"')
                    {
                        sb.Append('"');
                        i += 2;

                        continue;
                    }

                    tail = rest[(i + 1)..].TrimStart();

                    value = sb.ToString();

                    return tail.Length is 0;
                }

                sb.Append(c);
                i++;
            }

            return false;
        }

        ReadOnlySpan<char> token = rest;
        int end = 0;

        while (end < token.Length && !char.IsWhiteSpace(token[end]))
            end++;

        tail = token[end..].TrimStart();
        string t = token[..end].ToString();

        value = t;

        return t.Length > 0 && tail.Length is 0;
    }

    private static bool LooksLikeRichScimFilter(ReadOnlySpan<char> filter)
    {
        string f = filter.ToString();

        static bool ContainsI(string s, string needle) =>
            s.Contains(needle, StringComparison.OrdinalIgnoreCase);

        if (ContainsI(f, " and "))
            return true;

        if (ContainsI(f, ")"))
            return true;

        if (ContainsI(f, " or "))
            return true;

        if (ContainsI(f, " not "))
            return true;

        return ContainsI(f, " ne ")
               || ContainsI(f, " co ")
               || ContainsI(f, " sw ")
               || ContainsI(f, " ew ")
               || ContainsI(f, " gt ")
               || ContainsI(f, " lt ")
               || ContainsI(f, " ge ")
               || ContainsI(f, " le ")
               || ContainsI(f, " pr");
    }
}
