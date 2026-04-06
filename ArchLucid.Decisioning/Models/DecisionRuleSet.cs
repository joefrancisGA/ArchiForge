using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ArchiForge.Decisioning.Models;

public sealed class DecisionRuleSet
{
    public string RuleSetId { get; set; } = "in-memory";
    public string Version { get; set; } = "1";
    public string RuleSetHash { get; set; } = string.Empty;
    public List<DecisionRule> Rules { get; set; } = [];

    public void ComputeHash()
    {
        string canonical = JsonSerializer.Serialize(new
        {
            RuleSetId,
            Version,
            Rules = Rules
                .OrderByDescending(r => r.Priority)
                .ThenBy(r => r.RuleId)
                .Select(r => new
                {
                    r.RuleId,
                    r.Name,
                    r.Priority,
                    r.IsMandatory,
                    r.AppliesToFindingType,
                    r.Action,
                    Criteria = r.Criteria.OrderBy(kv => kv.Key).ToArray()
                })
                .ToArray()
        });

        using SHA256 sha = SHA256.Create();
        byte[] bytes = Encoding.UTF8.GetBytes(canonical);
        RuleSetHash = Convert.ToHexString(sha.ComputeHash(bytes));
    }
}

