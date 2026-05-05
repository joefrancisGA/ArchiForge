using ArchLucid.Application.Architecture;
using ArchLucid.Contracts.DecisionTraces;
using ArchLucid.Contracts.Manifest;

using FluentAssertions;

namespace ArchLucid.Application.Tests.Architecture;

public sealed class AuthorityCommitTraceabilityRulesTests
{
    [SkippableFact]
    public void GetLinkageGaps_returns_empty_when_trace_ids_align_with_RuleAudit()
    {
        Guid decisionTraceId = Guid.Parse("AABBCCDDEEFF00112233445566778899");
        GoldenManifest m = new()
        {
            RunId = "n",
            SystemName = "s",
            Services = [],
            Datastores = [],
            Relationships = [],
            Governance = new(),
            Metadata = new ManifestMetadata { ManifestVersion = "v1", DecisionTraceIds = [decisionTraceId.ToString("N")], },
        };

        RuleAuditTrace trace = RuleAuditTrace.From(
            new RuleAuditTracePayload { DecisionTraceId = decisionTraceId, RunId = Guid.NewGuid(), });

        IReadOnlyList<string> gaps = AuthorityCommitTraceabilityRules.GetLinkageGaps(m, [trace]);
        gaps.Should().BeEmpty();
    }

    [SkippableFact]
    public void GetLinkageGaps_reports_mismatch()
    {
        GoldenManifest m = new()
        {
            RunId = "n",
            SystemName = "s",
            Services = [],
            Datastores = [],
            Relationships = [],
            Governance = new(),
            Metadata = new ManifestMetadata { ManifestVersion = "v1", DecisionTraceIds = ["aabbccdd"], },
        };

        Guid other = Guid.NewGuid();
        RuleAuditTrace trace = RuleAuditTrace.From(
            new RuleAuditTracePayload { DecisionTraceId = other, RunId = Guid.NewGuid(), });

        IReadOnlyList<string> gaps = AuthorityCommitTraceabilityRules.GetLinkageGaps(m, [trace]);
        gaps.Should().NotBeEmpty();
    }
}
