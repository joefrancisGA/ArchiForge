using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ArchiForge.Decisioning.Compliance.Models;

namespace ArchiForge.Decisioning.Compliance.Loaders;

public class FileComplianceRulePackLoader(string filePath) : IComplianceRulePackLoader
{
    public async Task<ComplianceRulePack> LoadAsync(CancellationToken ct)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Compliance rule pack not found: {filePath}");

        var json = await File.ReadAllTextAsync(filePath, ct);

        var doc = JsonSerializer.Deserialize<ComplianceRulePackDocument>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

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
        var bytes = Encoding.UTF8.GetBytes(content);
        return Convert.ToHexString(SHA256.HashData(bytes));
    }
}
