using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ArchiForge.Decisioning.Compliance.Models;

namespace ArchiForge.Decisioning.Compliance.Loaders;

public class FileComplianceRulePackLoader : IComplianceRulePackLoader
{
    private readonly string _filePath;

    public FileComplianceRulePackLoader(string filePath)
    {
        _filePath = filePath;
    }

    public async Task<ComplianceRulePack> LoadAsync(CancellationToken ct)
    {
        if (!File.Exists(_filePath))
            throw new FileNotFoundException($"Compliance rule pack not found: {_filePath}");

        var json = await File.ReadAllTextAsync(_filePath, ct);

        var doc = JsonSerializer.Deserialize<ComplianceRulePackDocument>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        if (doc is null)
            throw new InvalidOperationException("Failed to deserialize compliance rule pack.");

        return new ComplianceRulePack
        {
            RulePackId = doc.RulePackId,
            Name = doc.Name,
            Version = doc.Version,
            SourcePath = _filePath,
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
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(content);
        return Convert.ToHexString(sha.ComputeHash(bytes));
    }
}
