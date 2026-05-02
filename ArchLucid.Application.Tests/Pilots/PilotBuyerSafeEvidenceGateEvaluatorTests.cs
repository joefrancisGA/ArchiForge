using ArchLucid.Application.Pilots;
using ArchLucid.Contracts.Architecture;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Manifest;
using ArchLucid.Contracts.Metadata;
using ArchLucid.Contracts.ValueReports;

using FluentAssertions;

namespace ArchLucid.Application.Tests.Pilots;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class PilotBuyerSafeEvidenceGateEvaluatorTests
{
    [Fact]
    public void Evaluate_CommittedStrongBaseline_IsComplete()
    {
        ArchitectureRun run = CommittedRun();

        PilotBuyerSafeEvidenceGateResult gate = PilotBuyerSafeEvidenceGateEvaluator.Evaluate(
            run,
            MinimalManifest(),
            MinimalDeltas(run) with { AuditRowCount = 3 },
            TenantCapturedSnapshot());

        gate.PublishingTier.Should().Be(PilotBuyerSafeEvidencePublishingTier.Complete);
        gate.Gaps.Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_DemoTenant_IsDemoOnlyWithGap()
    {
        ArchitectureRun run = CommittedRun();
        PilotRunDeltas deltas = MinimalDeltas(run) with { IsDemoTenant = true };

        PilotBuyerSafeEvidenceGateResult gate = PilotBuyerSafeEvidenceGateEvaluator.Evaluate(
            run,
            MinimalManifest(),
            deltas,
            TenantCapturedSnapshot());

        gate.PublishingTier.Should().Be(PilotBuyerSafeEvidencePublishingTier.DemoOnly);
        gate.Gaps.Should().Contain(g => g.Contains("Seeded/demo", StringComparison.Ordinal));
    }

    [Fact]
    public void Evaluate_NoManifest_IsPartial()
    {
        ArchitectureRun run = new()
        {
            RunId = "r1",
            RequestId = "q",
            Status = ArchitectureRunStatus.ReadyForCommit,
            CreatedUtc = DateTime.UtcNow,
        };

        PilotRunDeltas deltas = MinimalDeltas(run);

        PilotBuyerSafeEvidenceGateResult gate =
            PilotBuyerSafeEvidenceGateEvaluator.Evaluate(run, null, deltas, TenantCapturedSnapshot());

        gate.PublishingTier.Should().Be(PilotBuyerSafeEvidencePublishingTier.Partial);
        gate.Gaps.Should().Contain(g => g.Contains("Committed golden manifest", StringComparison.Ordinal));
    }

    [Fact]
    public void Evaluate_DefaultRoiBaseline_AddsRoiGapAndPartial()
    {
        ArchitectureRun run = CommittedRun();
        ValueReportSnapshot snap = MinimalSnapshotWith(ReviewCycleBaselineProvenance.DefaultedFromRoiModelOptions);

        PilotBuyerSafeEvidenceGateResult gate = PilotBuyerSafeEvidenceGateEvaluator.Evaluate(
            run,
            MinimalManifest(),
            MinimalDeltas(run),
            snap);

        gate.PublishingTier.Should().Be(PilotBuyerSafeEvidencePublishingTier.Partial);
        gate.Gaps.Should().Contain(g => g.Contains("ROI comparative", StringComparison.Ordinal));
    }

    [Fact]
    public void Evaluate_UnresolvedEvidenceChain_IsPartial()
    {
        ArchitectureRun run = CommittedRun();

        PilotBuyerSafeEvidenceGateResult gate = PilotBuyerSafeEvidenceGateEvaluator.Evaluate(
            run,
            MinimalManifest(),
            MinimalDeltas(run) with
            {
                AuditRowCount = 4,
                TopFindingId = "f-demo",
                TopFindingEvidenceChain = null,
            },
            TenantCapturedSnapshot());

        gate.PublishingTier.Should().Be(PilotBuyerSafeEvidencePublishingTier.Partial);
        gate.Gaps.Should().Contain(g => g.Contains("evidence-chain pointers", StringComparison.Ordinal));
    }

    private static ArchitectureRun CommittedRun() =>
        new()
        {
            RunId = "run-a",
            RequestId = "req-a",
            Status = ArchitectureRunStatus.Committed,
            CreatedUtc = new DateTime(2026, 4, 1, 12, 0, 0, DateTimeKind.Utc),
            CompletedUtc = new DateTime(2026, 4, 1, 13, 0, 0, DateTimeKind.Utc),
            RealModeFellBackToSimulator = false,
        };

    private static GoldenManifest MinimalManifest() =>
        new()
        {
            RunId = "run-a",
            SystemName = "Sys",
            Metadata = new ManifestMetadata
            {
                ManifestVersion = "v1",
                CreatedUtc = new DateTime(2026, 4, 1, 13, 0, 0, DateTimeKind.Utc),
            },
            Governance = new ManifestGovernance(),
        };

    private static PilotRunDeltas MinimalDeltas(ArchitectureRun run)
    {
        GoldenManifest m = MinimalManifest();

        return new PilotRunDeltas
        {
            RunCreatedUtc = run.CreatedUtc,
            ManifestCommittedUtc = m.Metadata.CreatedUtc,
            TimeToCommittedManifest = m.Metadata.CreatedUtc - run.CreatedUtc,
            AuditRowCount = 2,
            IsDemoTenant = false,
            TopFindingId = null,
        };
    }

    private static ValueReportSnapshot TenantCapturedSnapshot() =>
        MinimalSnapshotWith(ReviewCycleBaselineProvenance.TenantSuppliedAtSignup);

    private static ValueReportSnapshot MinimalSnapshotWith(ReviewCycleBaselineProvenance provenance)
    {
        Guid tid = Guid.Parse("11111111-1111-1111-1111-111111111111");

        return new ValueReportSnapshot(
            TenantId: tid,
            WorkspaceId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
            ProjectId: Guid.Parse("33333333-3333-3333-3333-333333333333"),
            PeriodFromUtc: DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
            PeriodToUtc: DateTimeOffset.Parse("2026-02-01T00:00:00Z"),
            RunStatusRows: [],
            RunsCompletedCount: 0,
            ManifestsCommittedCount: 0,
            GovernanceEventsHandledCount: 0,
            DriftAlertEventsCaughtCount: 0,
            EstimatedArchitectHoursSavedFromManifests: 0m,
            EstimatedArchitectHoursSavedFromGovernanceEvents: 0m,
            EstimatedArchitectHoursSavedFromDriftEvents: 0m,
            EstimatedTotalArchitectHoursSaved: 0m,
            EstimatedLlmCostForWindowUsd: 0m,
            EstimatedLlmCostMethodologyNote: "",
            AnnualizedHoursValueUsd: 0m,
            AnnualizedLlmCostUsd: 0m,
            BaselineAnnualSubscriptionAndOpsCostUsdFromRoiModel: 0m,
            NetAnnualizedValueVersusRoiBaselineUsd: 0m,
            RoiAnnualizedPercentVersusRoiBaseline: 10m,
            TenantBaselineReviewCycleHours: 8m,
            TenantBaselineReviewCycleSource: "signup",
            TenantBaselineReviewCycleCapturedUtc: DateTimeOffset.Parse("2026-04-01T12:00:00Z"),
            MeasuredAverageReviewCycleHoursForWindow: 8m,
            MeasuredReviewCycleSampleSize: 2,
            ReviewCycleBaselineProvenance: provenance,
            ReviewCycleHoursDelta: 2m,
            ReviewCycleHoursDeltaPercent: 10m,
            FindingFeedbackNetScore: 0,
            FindingFeedbackVoteCount: 0,
            TenantBaselineManualPrepHoursPerReview: null,
            TenantBaselinePeoplePerReview: null);
    }
}
