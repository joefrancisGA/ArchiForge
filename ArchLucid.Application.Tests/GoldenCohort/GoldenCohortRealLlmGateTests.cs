using System.Text.Json;

using ArchLucid.Core.GoldenCorpus;

namespace ArchLucid.Application.Tests.GoldenCohort;

/// <summary>
/// Lightweight checks for the optional nightly real-LLM gate job (no Azure OpenAI calls; budget probe runs separately).
/// </summary>
public sealed class GoldenCohortRealLlmGateTests
{
    [Fact]
    public void Cohort_contract_fixture_still_present_for_gate_job()
    {
        string cohortPath = Path.Combine(AppContext.BaseDirectory, "golden-cohort", "cohort.json");
        Assert.True(File.Exists(cohortPath), $"Missing {cohortPath} — gate job assumes cohort contract tests passed.");

        GoldenCohortDocument document = GoldenCohortDocument.Load(cohortPath);
        Assert.Equal(20, document.Items.Count);
    }

    [Fact]
    public void Usage_mtd_ledger_template_is_valid_json()
    {
        string ledgerPath = Path.Combine(AppContext.BaseDirectory, "golden-cohort", "usage-mtd.json");
        Assert.True(File.Exists(ledgerPath), $"Missing {ledgerPath} — copy from tests/golden-cohort via csproj link.");

        using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(ledgerPath));
        Assert.Equal(1, doc.RootElement.GetProperty("schemaVersion").GetInt32());
        Assert.True(doc.RootElement.TryGetProperty("entries", out JsonElement entries));
        Assert.Equal(JsonValueKind.Array, entries.ValueKind);
    }
}
