using System.Diagnostics;

using ArchLucid.ArtifactSynthesis.Models;
using ArchLucid.ContextIngestion.Models;
using ArchLucid.Contracts.DecisionTraces;
using ArchLucid.Core.Scoping;
using ArchLucid.Core.Transactions;
using ArchLucid.Decisioning.Models;
using ArchLucid.KnowledgeGraph.Models;
using ArchLucid.KnowledgeGraph.Services;
using ArchLucid.Persistence.Models;

namespace ArchLucid.Persistence.Orchestration.Pipeline;

/// <summary>
///     Mutable state passed through ordered <see cref="IAuthorityPipelineStage" /> executions inside one unit of work.
/// </summary>
public sealed class AuthorityPipelineContext
{
    public required RunRecord Run
    {
        get;
        init;
    }

    public required ContextIngestionRequest Request
    {
        get;
        set;
    }

    public required IArchLucidUnitOfWork UnitOfWork
    {
        get;
        init;
    }

    public required ScopeContext Scope
    {
        get;
        init;
    }

    public Activity? RunActivity
    {
        get;
        init;
    }

    public ContextSnapshot? PriorCommittedContext
    {
        get;
        set;
    }

    public ContextSnapshot? ContextSnapshot
    {
        get;
        set;
    }

    public GraphSnapshotResolutionResult? GraphResolution
    {
        get;
        set;
    }

    public GraphSnapshot? GraphSnapshot
    {
        get;
        set;
    }

    public FindingsSnapshot? FindingsSnapshot
    {
        get;
        set;
    }

    public GoldenManifest? Manifest
    {
        get;
        set;
    }

    public DecisionTrace? Trace
    {
        get;
        set;
    }

    public ArtifactBundle? ArtifactBundle
    {
        get;
        set;
    }
}
