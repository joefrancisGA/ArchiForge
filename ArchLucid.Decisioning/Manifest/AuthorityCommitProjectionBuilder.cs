using ArchLucid.Decisioning.Interfaces;
using ArchLucid.Decisioning.Models;

using DmSec = ArchLucid.Decisioning.Manifest.Sections;
using Cm = ArchLucid.Contracts.Manifest;

namespace ArchLucid.Decisioning.Manifest;

/// <inheritdoc cref="IAuthorityCommitProjectionBuilder" />
public sealed class AuthorityCommitProjectionBuilder : IAuthorityCommitProjectionBuilder
{
    public Task<Cm.GoldenManifest> BuildAsync(
        GoldenManifest source,
        AuthorityCommitProjectionInput input,
        CancellationToken cancellationToken = default)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));

        if (input is null)
            throw new ArgumentNullException(nameof(input));

        if (string.IsNullOrWhiteSpace(input.SystemName))
            throw new ArgumentException("SystemName is required for coordinator-shaped projection.", nameof(input));

        cancellationToken.ThrowIfCancellationRequested();

        Cm.GoldenManifest result = new()
        {
            RunId = source.RunId.ToString("N"),
            SystemName = input.SystemName,
            Services = [.. source.Topology.Services],
            Datastores = [.. source.Topology.Datastores],
            Relationships = [.. source.Topology.Relationships],
            Governance = MapGovernance(source),
            Metadata = MapMetadata(source)
        };
        return Task.FromResult(result);
    }

    private static Cm.ManifestGovernance MapGovernance(GoldenManifest source)
    {
        List<string> complianceTags = source.Compliance.Controls
            .Select(c => c.ControlName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        List<string> policyConstraints = source.Policy.Violations
            .Select(v => v.ControlName)
            .Concat(source.Policy.Notes)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        List<string> required = source.Security.Controls
            .Where(c => string.Equals(c.Status, "missing", StringComparison.OrdinalIgnoreCase) is false)
            .Select(c => c.ControlName)
            .Concat(source.Policy.SatisfiedControls.Select(s => s.ControlName))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        string risk = "Moderate";
        if (source.Cost.CostRisks.Count > 0)
            risk = "High";
        if (source.UnresolvedIssues.Items.Count == 0 && source.Cost.CostRisks.Count == 0)
            risk = "Low";

        string costTier = source.Cost.MaxMonthlyCost is { } m && m > 10000m ? "High" : "Moderate";

        return new Cm.ManifestGovernance
        {
            ComplianceTags = complianceTags,
            PolicyConstraints = policyConstraints,
            RequiredControls = required,
            RiskClassification = risk,
            CostClassification = costTier
        };
    }

    private static Cm.ManifestMetadata MapMetadata(GoldenManifest source)
    {
        DmSec.ManifestMetadata meta = source.Metadata;

        string manifestVersion = "v1";
        if (string.IsNullOrWhiteSpace(meta.Version) is false)
            manifestVersion = meta.Version.StartsWith("v", StringComparison.OrdinalIgnoreCase)
                ? meta.Version
                : $"v{meta.Version}";

        return new Cm.ManifestMetadata
        {
            ManifestVersion = manifestVersion,
            ParentManifestVersion = null,
            ChangeDescription = meta.Summary,
            DecisionTraceIds = [source.DecisionTraceId.ToString("N")],
            CreatedUtc = source.CreatedUtc
        };
    }
}
