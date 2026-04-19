using ArchLucid.Contracts.DecisionTraces;
using ArchLucid.Decisioning.Manifest.Builders;
using ArchLucid.Decisioning.Models;
using ArchLucid.Decisioning.Rules;
using ArchLucid.KnowledgeGraph.Models;

using FluentAssertions;

using FsCheck.Xunit;

namespace ArchLucid.Decisioning.Tests;

/// <summary>
/// Property checks that <see cref="DefaultGoldenManifestBuilder"/> preserves scope identifiers on the happy path.
/// </summary>
[Trait("Suite", "Core")]
public sealed class DefaultGoldenManifestBuilderManifestIdentityPropertyTests
{
#pragma warning disable xUnit1031 // FsCheck properties are synchronous; rule provider is async.

    [Property(MaxTest = 40)]
    public void Build_with_empty_findings_preserves_run_context_and_snapshot_ids(Guid runId, Guid ctxId)
    {
        Guid graphSnapshotId = Guid.NewGuid();
        Guid findingsSnapshotId = Guid.NewGuid();

        GraphSnapshot graph = new()
        {
            GraphSnapshotId = graphSnapshotId,
            ContextSnapshotId = ctxId,
            RunId = runId,
            Nodes = [],
            Edges = [],
        };

        FindingsSnapshot findings = new()
        {
            FindingsSnapshotId = findingsSnapshotId,
            RunId = runId,
            ContextSnapshotId = ctxId,
            GraphSnapshotId = graphSnapshotId,
            Findings = [],
        };

        DecisionTrace trace = RuleAuditTrace.From(
            new RuleAuditTracePayload
            {
                DecisionTraceId = Guid.NewGuid(),
                RunId = runId,
            });

        DecisionRuleSet ruleSet = new InMemoryDecisionRuleProvider()
            .GetRuleSetAsync(CancellationToken.None)
            .GetAwaiter()
            .GetResult();

        GoldenManifest manifest = new DefaultGoldenManifestBuilder().Build(
            runId,
            ctxId,
            graph,
            findings,
            trace,
            ruleSet);

        manifest.RunId.Should().Be(runId);
        manifest.ContextSnapshotId.Should().Be(ctxId);
        manifest.GraphSnapshotId.Should().Be(graphSnapshotId);
        manifest.FindingsSnapshotId.Should().Be(findingsSnapshotId);
    }

#pragma warning restore xUnit1031
}
