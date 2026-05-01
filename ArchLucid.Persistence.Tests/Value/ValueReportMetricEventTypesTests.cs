using ArchLucid.Core.Audit;
using ArchLucid.Persistence.Value;

namespace ArchLucid.Persistence.Tests.Value;

[Trait("Category", "Unit")]
public sealed class ValueReportMetricEventTypesTests
{
    [Fact]
    public void GovernanceEventTypes_covers_primary_resolution_and_pre_commit_signals()
    {
        IReadOnlyList<string> governance = ValueReportMetricEventTypes.GovernanceEventTypes;

        governance.Should().Contain(AuditEventTypes.GovernanceResolutionExecuted);
        governance.Should().Contain(AuditEventTypes.GovernancePreCommitBlocked);
        governance.Should().Contain(AuditEventTypes.Baseline.Governance.ApprovalRequestSubmitted);
        governance.Should().HaveCountGreaterThan(5);
    }

    [Fact]
    public void DriftAlertEventTypes_covers_operational_signals()
    {
        IReadOnlyList<string> drift = ValueReportMetricEventTypes.DriftAlertEventTypes;

        drift.Should().Contain(AuditEventTypes.AlertTriggered);
        drift.Should().Contain(AuditEventTypes.AlertResolved);
        drift.Should().Contain(AuditEventTypes.CompositeAlertTriggered);
        drift.Should().HaveCount(3);
    }
}
