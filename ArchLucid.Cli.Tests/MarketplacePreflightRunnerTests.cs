using ArchLucid.Cli.Commands;

using FluentAssertions;

namespace ArchLucid.Cli.Tests;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class MarketplacePreflightRunnerTests
{
    [Fact]
    public void Evaluate_real_repo_passes_all_steps()
    {
        string root = FindRepoRoot();

        IReadOnlyList<MarketplacePreflightStepResult> steps = MarketplacePreflightRunner.Evaluate(root);

        steps.Should().NotBeEmpty();
        steps.Should().OnlyContain(static s => s.Passed);
    }

    [Fact]
    public void Evaluate_minimal_fake_repo_with_drift_fails()
    {
        string temp = Path.Combine(Path.GetTempPath(), "archlucid-preflight-" + Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(Path.Combine(temp, "docs", "go-to-market"));
        Directory.CreateDirectory(Path.Combine(temp, "docs"));
        Directory.CreateDirectory(Path.Combine(temp, "ArchLucid.Api"));

        File.WriteAllText(
            Path.Combine(temp, "docs", "go-to-market", "PRICING_PHILOSOPHY.md"),
            "no canonical row here\n");

        File.WriteAllText(
            Path.Combine(temp, "docs", "go-to-market", "MARKETPLACE_PUBLICATION.md"),
            "wrong triple `Team` / `Pro` / `Enterprise`\n");

        File.WriteAllText(
            Path.Combine(temp, "docs", "AZURE_MARKETPLACE_SAAS_OFFER.md"),
            "Professional tier docs must not use deprecated `Pro` slug.\n");

        File.WriteAllText(Path.Combine(temp, "docs", "BILLING.md"), "missing routes on purpose\n");

        File.WriteAllText(Path.Combine(temp, "ArchLucid.Api", "appsettings.json"), "{}\n");

        try
        {
            IReadOnlyList<MarketplacePreflightStepResult> steps = MarketplacePreflightRunner.Evaluate(temp);

            steps.Should().Contain(static s => s.Id == "pricing_canonical_packaging_row" && !s.Passed);
            steps.Should().Contain(static s => s.Id == "publication_plan_tier_triple" && !s.Passed);
            steps.Should().Contain(static s => s.Id == "azure_no_pro_tier_slug" && !s.Passed);
        }
        finally
        {
            Directory.Delete(temp, recursive: true);
        }
    }

    private static string FindRepoRoot()
    {
        string? root = CliRepositoryRootResolver.TryResolveRepositoryRoot();

        if (root is null)
            throw new InvalidOperationException("Run tests from within the ArchLucid repository.");

        return root;
    }
}
