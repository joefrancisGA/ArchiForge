using System.Security.Cryptography;
using System.Text;

namespace ArchLucid.Cli.Commands;

/// <summary>Produces Mermaid flowchart source from provenance REST graph JSON.</summary>
internal static class GraphWireMermaidFormatter
{
    private const string Unknown = "?";

    internal static string ToFlowchart(GraphWireModel vm)
    {
        StringBuilder sb = new();
        sb.AppendLine("flowchart LR");

        Dictionary<string, GraphNodeWire?> byId =
            vm.Nodes
                .Where(static n => !string.IsNullOrWhiteSpace(n.Id))
                .GroupBy(static n => n.Id.Trim(), StringComparer.Ordinal)
                .ToDictionary(static g => g.Key, static g => (GraphNodeWire?)g.First(), StringComparer.Ordinal);

        HashSet<string> edgeReferenced = [];

        foreach (GraphEdgeWire e in vm.Edges.OfType<GraphEdgeWire>().Where(e =>
                     !string.IsNullOrWhiteSpace(e.Source) && !string.IsNullOrWhiteSpace(e.Target)))
        {
            edgeReferenced.Add(e.Source.Trim());
            edgeReferenced.Add(e.Target.Trim());
        }

        IEnumerable<string> stubIds = edgeReferenced.Where(id => !byId.ContainsKey(id));

        foreach (string stub in stubIds.Distinct(StringComparer.Ordinal))

            byId[stub] = null;

        foreach (KeyValuePair<string, GraphNodeWire?> kv in byId.OrderBy(static kv => kv.Key, StringComparer.Ordinal))
        {
            string mid = ToMermaidNodeId(kv.Key);
            GraphNodeWire? n = kv.Value;
            string lbl = n is null ? Unknown : FormatNodeLabel(n);
            sb.Append($"{mid}[\"");
            sb.Append(EscapeQuotes(lbl));
            sb.AppendLine("\"]");
        }

        foreach (GraphEdgeWire e in vm.Edges.OfType<GraphEdgeWire>().Where(e =>
                     !string.IsNullOrWhiteSpace(e.Source) && !string.IsNullOrWhiteSpace(e.Target)))
        {
            if (string.IsNullOrWhiteSpace(e.Source) || string.IsNullOrWhiteSpace(e.Target))
                continue;

            string s = ToMermaidNodeId(e.Source.Trim());
            string t = ToMermaidNodeId(e.Target.Trim());
            sb.Append(s);

            string edgeLbl = EscapeQuotes(e.Type.Trim());
            if (edgeLbl.Length != 0)

                sb.Append($" -- \"{edgeLbl}\" --> ");
            else

                sb.Append(" --> ");

            sb.AppendLine(t);
        }

        return sb.ToString().TrimEnd();
    }

    private static string FormatNodeLabel(GraphNodeWire n)
    {
        StringBuilder lbl = new();
        lbl.Append(n.Label.Trim());

        if (!string.IsNullOrWhiteSpace(n.Type))
        {
            if (lbl.Length != 0)
                lbl.Append(" :: ");

            lbl.Append(n.Type.Trim());
        }

        string s = lbl.ToString();

        return s.Length != 0 ? s : Unknown;
    }

    private static string EscapeQuotes(string? s)
    {
        return string.IsNullOrEmpty(s)
            ? string.Empty
            : s.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "'", StringComparison.Ordinal);
    }

    /// <remarks>
    ///     Stable alphanumeric id avoids Mermaid lexer issues when source ids contain GUID punctuation.
    /// </remarks>
    private static string ToMermaidNodeId(string raw)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw.Trim()));

        return "n" + Convert.ToHexStringLower(hash.AsSpan(..8));
    }
}
