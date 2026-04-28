namespace ArchLucid.Architecture.Tests;

/// <summary>Namespace prefixes used with NetArchTest <c>HaveDependencyOn</c> / <c>HaveDependencyOnAny</c> (prefix matching).</summary>
internal static class ArchitectureConstraintNamespaces
{
    /// <summary>
    /// Every first-party <c>ArchLucid.*</c> area except <c>ArchLucid.Core</c>.
    /// <para><b>Do not</b> add <c>ArchLucid.Contracts</c> or <c>ArchLucid.Contracts.Abstractions</c> here: <c>ArchLucid.Core</c> intentionally
    /// references <c>ArchLucid.Contracts</c> for shared DTOs (see <c>ArchLucid.Core.csproj</c>). NetArchTest would flag
    /// <c>RunExplanationSummary</c>, <c>SanitizedLoggerInformationExtensions</c>, and similar types if those prefixes were forbidden.</para>
    /// </summary>
    internal static readonly string[] ForbiddenFromCore =
    [
        "ArchLucid.AgentRuntime",
        "ArchLucid.AgentSimulator",
        "ArchLucid.Api",
        "ArchLucid.Api.Client",
        "ArchLucid.Application",
        "ArchLucid.ArtifactSynthesis",
        "ArchLucid.Backfill",
        "ArchLucid.Cli",
        "ArchLucid.ContextIngestion",
        "ArchLucid.Decisioning",
        "ArchLucid.Host",
        "ArchLucid.KnowledgeGraph",
        "ArchLucid.Persistence",
        "ArchLucid.Provenance",
        "ArchLucid.Retrieval",
        "ArchLucid.TestSupport",
        "ArchLucid.Worker",
    ];

    /// <summary>All <c>ArchLucid.*</c> except <c>ArchLucid.Contracts</c> (Contracts leaf assembly).</summary>
    internal static readonly string[] ForbiddenFromContracts =
    [
        "ArchLucid.AgentRuntime",
        "ArchLucid.AgentSimulator",
        "ArchLucid.Api",
        "ArchLucid.Api.Client",
        "ArchLucid.Application",
        "ArchLucid.ArtifactSynthesis",
        "ArchLucid.Backfill",
        "ArchLucid.Cli",
        "ArchLucid.ContextIngestion",
        "ArchLucid.Contracts.Abstractions",
        "ArchLucid.Core",
        "ArchLucid.Decisioning",
        "ArchLucid.Host",
        "ArchLucid.KnowledgeGraph",
        "ArchLucid.Persistence",
        "ArchLucid.Provenance",
        "ArchLucid.Retrieval",
        "ArchLucid.TestSupport",
        "ArchLucid.Worker",
    ];

    /// <summary>Abstractions may reference Contracts; nothing else under ArchLucid.</summary>
    internal static readonly string[] ForbiddenFromContractsAbstractions =
    [
        "ArchLucid.AgentRuntime",
        "ArchLucid.AgentSimulator",
        "ArchLucid.Api",
        "ArchLucid.Api.Client",
        "ArchLucid.Application",
        "ArchLucid.ArtifactSynthesis",
        "ArchLucid.Backfill",
        "ArchLucid.Cli",
        "ArchLucid.ContextIngestion",
        "ArchLucid.Core",
        "ArchLucid.Decisioning",
        "ArchLucid.Host",
        "ArchLucid.KnowledgeGraph",
        "ArchLucid.Persistence",
        "ArchLucid.Provenance",
        "ArchLucid.Retrieval",
        "ArchLucid.TestSupport",
        "ArchLucid.Worker",
    ];
}
