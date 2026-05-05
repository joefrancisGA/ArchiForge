using System.Globalization;
using System.Text;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ArchLucid.Cli;

/// <summary>
///     Turns JSON instance locations from schema evaluation into Newtonsoft <see cref="JToken.SelectToken(string)" />
///     paths and resolves editor line/column hints when the manifest was parsed with Newtonsoft line tracking.
/// </summary>
internal static class JsonPointerLineLocator
{
    internal static string InstancePointerToNewtonsoftSelectPath(string instancePointer)
    {
        string trimmed = instancePointer.Trim();

        if (trimmed.Length == 0 || trimmed == "#" || string.Equals(trimmed, "(root)", StringComparison.Ordinal))
            return "$";

        if (trimmed.StartsWith('#'))
            trimmed = trimmed[1..];

        string pointer = trimmed.StartsWith('/') ? trimmed[1..] : trimmed;
        string[] segments = pointer.Split('/');
        StringBuilder sb = new();

        foreach (string escaped in segments)
        {
            if (escaped.Length == 0)
                continue;

            string segment = UnescapeSegment(escaped);

            if (long.TryParse(segment, NumberStyles.None, CultureInfo.InvariantCulture, out _))
            {
                sb.Append('[').Append(segment).Append(']');
            }
            else if (sb.Length == 0)
            {
                sb.Append(segment);
            }
            else
            {
                sb.Append('.').Append(segment);
            }
        }

        return sb.Length == 0 ? "$" : sb.ToString();
    }

    internal static bool TryGetNewtonsoftSourceLine(JToken root, string instancePointer, out int lineNumber,
        out int column)
    {
        lineNumber = 0;
        column = 0;

        string path = InstancePointerToNewtonsoftSelectPath(instancePointer);
        JToken? node = path == "$" ? root : root.SelectToken(path);

        if (node is null)
            return false;

        if (node is IJsonLineInfo lineInfo && lineInfo.HasLineInfo())
        {
            lineNumber = lineInfo.LineNumber;
            column = lineInfo.LinePosition;
            return true;
        }

        return false;
    }

    private static string UnescapeSegment(string escaped)
    {
        return escaped.Replace("~1", "/", StringComparison.Ordinal).Replace("~0", "~", StringComparison.Ordinal);
    }
}
