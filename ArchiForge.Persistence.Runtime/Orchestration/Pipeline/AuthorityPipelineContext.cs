using System.Diagnostics;

using ArchiForge.ArtifactSynthesis.Models;
using ArchiForge.ContextIngestion.Models;
using ArchiForge.Core.Scoping;
using ArchiForge.Decisioning.Models;
using ArchiForge.KnowledgeGraph.Models;
using ArchiForge.KnowledgeGraph.Services;
using ArchiForge.Persistence.Models;
using ArchiForge.Persistence.Transactions;

namespace ArchiForge.Persistence.Orchestration.Pipeline;

/// <summary>
/// Mutable state passed through ordered <see cref="IAuthorityPipelineStage"/> executions inside one unit of work.
/// </summary>
public sealed class AuthorityPipelineContext
{
    public required RunRecord Run { get; init; }

    public required ContextIngestionRequest Request { get; set; }

    public required IArchiForgeUnitOfWork UnitOfWork { get; init; }

    public required ScopeContext Scope { get; init; }

    public Activity? RunActivity { get; init; }

    public ContextSnapshot? PriorCommittedContext { get; set; }

    public ContextSnapshot? ContextSnapshot { get; set; }

    public GraphSnapshotResolutionResult? GraphResolution { get; set; }

    public GraphSnapshot? GraphSnapshot { get; set; }

    public FindingsSnapshot? FindingsSnapshot { get; set; }

    public GoldenManifest? Manifest { get; set; }

    public RuleAuditTrace? Trace { get; set; }

    public ArtifactBundle? ArtifactBundle { get; set; }
}
