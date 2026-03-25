using System.Text.RegularExpressions;

using ArchiForge.ContextIngestion.Models;

namespace ArchiForge.ContextIngestion.Infrastructure;

public class SimpleTerraformDeclarationParser : IInfrastructureDeclarationParser
{
    /// <summary>Lightweight line-based match for <c>resource "type" "name"</c> blocks (not full HCL).</summary>
    private static readonly Regex ResourceRegex = new(
        @"resource\s+""(?<type>[^""]+)""\s+""(?<name>[^""]+)""",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public bool CanParse(string format) =>
        string.Equals(format, "simple-terraform", StringComparison.OrdinalIgnoreCase);

    public Task<IReadOnlyList<CanonicalObject>> ParseAsync(
        InfrastructureDeclarationReference declaration,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(declaration);
        _ = ct;
        MatchCollection matches = ResourceRegex.Matches(declaration.Content ?? string.Empty);
        List<CanonicalObject> results = new();

        foreach (Match match in matches)
        {
            string terraformType = match.Groups["type"].Value;
            string name = match.Groups["name"].Value;

            if (string.IsNullOrWhiteSpace(terraformType) || string.IsNullOrWhiteSpace(name))
                continue;

            string objectType = ResolveObjectType(terraformType);

            results.Add(new CanonicalObject
            {
                ObjectType = objectType,
                Name = name,
                SourceType = "InfrastructureDeclaration",
                SourceId = declaration.DeclarationId,
                Properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["terraformType"] = terraformType
                }
            });
        }

        return Task.FromResult<IReadOnlyList<CanonicalObject>>(results);
    }

    private static string ResolveObjectType(string terraformType)
    {
        string normalized = terraformType.ToLowerInvariant();

        if (normalized.Contains("key_vault", StringComparison.Ordinal) ||
            normalized.Contains("firewall", StringComparison.Ordinal) ||
            normalized.Contains("network_security_group", StringComparison.Ordinal))
        {
            return "SecurityBaseline";
        }

        if (normalized.Contains("policy", StringComparison.Ordinal))
        {
            return "PolicyControl";
        }

        return "TopologyResource";
    }
}
