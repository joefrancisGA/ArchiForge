using System.Text.RegularExpressions;

using ArchLucid.Core.GoldenCorpus;


namespace ArchLucid.Application.Tests.GoldenCohort;

/// <summary>
/// Validates the committed golden cohort JSON (N=20) used for nightly simulator drift automation.
/// </summary>
public sealed class GoldenCohortContractTests(ITestOutputHelper output)
{
    [Fact]
    public void Cohort_json_exists_has_twenty_items_and_valid_sha_fields()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "golden-cohort", "cohort.json");
        Assert.True(File.Exists(path), $"Missing {path} â€” ensure cohort.json is copied to output.");

        GoldenCohortDocument document = GoldenCohortDocument.Load(path);

        Assert.Equal(1, document.SchemaVersion);
        Assert.Equal(20, document.Items.Count);

        Regex sha = new("^[0-9a-fA-F]{64}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

        bool allShaPlaceholders = document.Items.TrueForAll(static item =>
            string.Equals(
                item.ExpectedCommittedManifestSha256.Trim(),
                GoldenCohortBaselineConstants.UnlockedManifestSha256Placeholder,
                StringComparison.OrdinalIgnoreCase));

        if (allShaPlaceholders)
        {
            output.WriteLine(
                "Baseline not yet locked â€” run `archlucid golden-cohort lock-baseline --write` against a Simulator API host "
                + "after explicit owner approval (set ARCHLUCID_GOLDEN_COHORT_BASELINE_LOCK_APPROVED=true for that shell only; "
                + "see docs/PENDING_QUESTIONS.md item 33).");
        }

        foreach (GoldenCohortItem item in document.Items)
        {
            Assert.False(string.IsNullOrWhiteSpace(item.Id), "Each item needs an id.");

            string hash = item.ExpectedCommittedManifestSha256.Trim();
            Assert.True(sha.IsMatch(hash), $"Item {item.Id}: expectedCommittedManifestSha256 must be 64 hex chars.");

            Assert.True(item.ExpectedFindingCategories.Count > 0, $"Item {item.Id}: expectedFindingCategories must not be empty.");
        }
    }
}
