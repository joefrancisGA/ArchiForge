using System.Text.Json;

using ArchLucid.ContextIngestion.Models;

using Microsoft.Extensions.Logging;

namespace ArchLucid.ContextIngestion.Infrastructure;

/// <summary>
/// Parses <c>terraform show -json</c> state output (Terraform JSON state representation) into
/// <see cref="CanonicalObject"/> rows aligned with other infrastructure declaration parsers.
/// </summary>
/// <remarks>
/// Clients paste the JSON into <see cref="InfrastructureDeclarationReference.Content"/> with
/// <see cref="InfrastructureDeclarationReference.Format"/> <c>terraform-show-json</c>.
/// </remarks>
public sealed class TerraformShowJsonInfrastructureDeclarationParser(
    ILogger<TerraformShowJsonInfrastructureDeclarationParser> logger) : IInfrastructureDeclarationParser
{
    public bool CanParse(string format) =>
        string.Equals(format, "terraform-show-json", StringComparison.OrdinalIgnoreCase);

    public Task<IReadOnlyList<CanonicalObject>> ParseAsync(
        InfrastructureDeclarationReference declaration,
        CancellationToken ct)
    {
        _ = ct;

        if (string.IsNullOrWhiteSpace(declaration.Content))
            return Task.FromResult<IReadOnlyList<CanonicalObject>>([]);

        List<CanonicalObject> results = [];

        try
        {
            using JsonDocument doc = JsonDocument.Parse(declaration.Content);
            JsonElement root = doc.RootElement;

            if (!root.TryGetProperty("values", out JsonElement values))
            {
                logger.LogWarning(
                    "Infrastructure declaration '{Name}' (terraform-show-json) has no 'values' root; expected terraform state JSON.",
                    declaration.Name);

                return Task.FromResult<IReadOnlyList<CanonicalObject>>([]);
            }

            if (values.TryGetProperty("root_module", out JsonElement rootModule))
                CollectFromModule(rootModule, declaration, results);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex,
                "Failed to parse infrastructure declaration '{Name}' (DeclarationId={DeclarationId}) as terraform-show-json; skipping.",
                declaration.Name,
                declaration.DeclarationId);

            return Task.FromResult<IReadOnlyList<CanonicalObject>>([]);
        }

        return Task.FromResult<IReadOnlyList<CanonicalObject>>(results);
    }

    private static void CollectFromModule(
        JsonElement module,
        InfrastructureDeclarationReference declaration,
        List<CanonicalObject> results)
    {
        if (module.TryGetProperty("resources", out JsonElement resources) && resources.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement res in resources.EnumerateArray())
                TryAddResource(res, declaration, results);
        }

        if (!module.TryGetProperty("child_modules", out JsonElement children) || children.ValueKind != JsonValueKind.Array)
            return;

        foreach (JsonElement child in children.EnumerateArray())
            CollectFromModule(child, declaration, results);
    }

    private static void TryAddResource(
        JsonElement res,
        InfrastructureDeclarationReference declaration,
        List<CanonicalObject> results)
    {
        if (!res.TryGetProperty("type", out JsonElement typeEl) || typeEl.ValueKind != JsonValueKind.String)
            return;

        string tfType = typeEl.GetString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(tfType))
            return;

        if (!res.TryGetProperty("name", out JsonElement nameEl) || nameEl.ValueKind != JsonValueKind.String)
            return;

        string name = nameEl.GetString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
            return;

        string objectType = ResolveObjectTypeFromTerraformType(tfType);

        Dictionary<string, string> properties = new(StringComparer.OrdinalIgnoreCase)
        {
            ["terraformType"] = tfType
        };

        if (res.TryGetProperty("provider_name", out JsonElement prov) && prov.ValueKind == JsonValueKind.String)
        {
            string? p = prov.GetString();
            if (!string.IsNullOrWhiteSpace(p))
                properties["providerName"] = p;
        }

        if (res.TryGetProperty("mode", out JsonElement mode) && mode.ValueKind == JsonValueKind.String)
        {
            string? m = mode.GetString();
            if (!string.IsNullOrWhiteSpace(m))
                properties["mode"] = m;
        }

        if (res.TryGetProperty("values", out JsonElement values) && values.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty prop in values.EnumerateObject())
            {
                if (properties.Count >= 24)
                    break;

                string key = SanitizePropertyKey(prop.Name);
                if (string.IsNullOrEmpty(key))
                    continue;

                string valueText = prop.Value.ValueKind switch
                {
                    JsonValueKind.String => prop.Value.GetString() ?? string.Empty,
                    JsonValueKind.Number => prop.Value.GetRawText(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    JsonValueKind.Null => string.Empty,
                    _ => prop.Value.GetRawText()
                };

                if (string.IsNullOrWhiteSpace(valueText))
                    continue;

                properties[$"tf.{key}"] = valueText.Length > 512 ? valueText[..512] : valueText;
            }
        }

        results.Add(new CanonicalObject
        {
            ObjectType = objectType,
            Name = $"{tfType}.{name}",
            SourceType = "InfrastructureDeclaration",
            SourceId = declaration.DeclarationId,
            Properties = properties
        });
    }

    private static string SanitizePropertyKey(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        ReadOnlySpan<char> s = name.AsSpan();
        Span<char> buffer = stackalloc char[s.Length];
        int w = 0;

        foreach (char c in s)
        {
            if (char.IsLetterOrDigit(c) || c is '_' or '-')
                buffer[w++] = c;
            else
                buffer[w++] = '_';
        }

        return new string(buffer[..w]);
    }

    private static string ResolveObjectTypeFromTerraformType(string tfType)
    {
        ReadOnlySpan<char> s = tfType.AsSpan();
        int slash = s.LastIndexOf('/');
        ReadOnlySpan<char> tail = slash >= 0 ? s[(slash + 1)..] : s;

        return tail.ToString().ToLowerInvariant() switch
        {
            "azurerm_key_vault" or "azurerm_firewall" or "azurerm_network_security_group" or "azurerm_key_vault_access_policy" =>
                "SecurityBaseline",
            "azurerm_policy_assignment" or "azurerm_policy_definition" => "PolicyControl",
            _ => "TopologyResource"
        };
    }
}
