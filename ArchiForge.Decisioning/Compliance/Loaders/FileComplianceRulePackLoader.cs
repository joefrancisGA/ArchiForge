using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using ArchiForge.Decisioning.Compliance.Models;

namespace ArchiForge.Decisioning.Compliance.Loaders;

public class FileComplianceRulePackLoader(string filePath) : IComplianceRulePackLoader
{
    private static readonly JsonSerializerOptions DeserializeOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<ComplianceRulePack> LoadAsync(CancellationToken ct)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Compliance rule pack not found: {filePath}");

        string json = await File.ReadAllTextAsync(filePath, ct);

        ComplianceRulePackDocument? doc;
        try
        {
            doc = JsonSerializer.Deserialize<ComplianceRulePackDocument>(json, DeserializeOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Compliance rule pack at '{filePath}' contains invalid JSON.", ex);
        }

        return doc is null
            ? throw new InvalidOperationException("Failed to deserialize compliance rule pack.")
            : new ComplianceRulePack
            {
                RulePackId = doc.RulePackId,
                Name = doc.Name,
                Version = doc.Version,
                SourcePath = filePath,
                RulePackHash = ComputeHash(json),
                Rules = doc.Rules.Select(x => new ComplianceRule
                {
                    RuleId = x.RuleId,
                    ControlId = x.ControlId,
                    ControlName = x.ControlName,
                    AppliesToCategory = x.AppliesToCategory,
                    RequiredNodeType = x.RequiredNodeType,
                    RequiredEdgeType = x.RequiredEdgeType,
                    Severity = x.Severity,
                    Description = x.Description
                }).ToList()
            };
    }

    private static string ComputeHash(string content)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(content);
        return Convert.ToHexString(SHA256.HashData(bytes));
    }
}
